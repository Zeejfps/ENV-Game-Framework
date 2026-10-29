using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection.Metadata;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp.ProjectDecompiler;
using ICSharpCode.Decompiler.Documentation;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;

namespace ZGF.ILSpy.Core;

/// <summary>Where an assembly read out of a package came from: the package, as listed, and its file there.</summary>
public sealed record PackagePlace(LoadedPackage Container, PackageFile File);

/// <summary>A folder of a listed package, whose files can be extracted: the package and the folder.</summary>
public sealed record PackageFolderPlace(LoadedPackage Container, PackageFolder Folder);

/// <summary>
/// A file known to an <see cref="AssemblyList"/>: one the user opened, or one loaded to resolve a
/// reference. It is a <see cref="LoadedModule"/> that was read, a <see cref="FailedAssembly"/> that
/// could not be, or a <see cref="LoadedPackage"/> whose files are browsed; no other kind can be made.
/// Its <see cref="Root"/> node, and every node under it, keep their identity for as long as this
/// object lives.
/// </summary>
public abstract class LoadedAssembly
{
    private static readonly TypeSystemOptions DefaultTypeSystemOptions = DecompilerTypeSystem.GetOptions(Settings.Create());
    private const string NamespaceIdPrefix = "N:";

    private readonly Lazy<DecompilerTypeSystem> _typeSystem;
    private readonly Lazy<DebugSymbols?> _debugSymbols;
    private readonly Lazy<bool> _canGeneratePdb;
    private readonly Lazy<ReferenceUsage> _referenceUsage;
    private readonly ConcurrentDictionary<TypeSystemOptions, Lazy<DecompilerTypeSystem>> _typeSystems = new();
    private readonly ConcurrentDictionary<EntityHandle, BrowseNode> _nodes = new();
    private readonly Lazy<BrowseNode> _root;
    private readonly MetadataFile? _file;
    private readonly AssemblyLoadException? _loadError;
    private BrowseNode? _rootInPackage;

    private protected LoadedAssembly(AssemblyList list, string filePath, MetadataFile? file, AssemblyLoadException? error, PackagePlace? place)
    {
        List = list;
        _file = file;
        _loadError = error;
        FilePath = filePath;
        InPackage = place;
        if (file != null && file.Metadata.IsAssembly)
        {
            var metadata = file.Metadata;
            var definition = metadata.GetAssemblyDefinition();
            Name = metadata.GetString(definition.Name);
            Version = definition.Version;
            FullName = file.FullName;
        }
        else
        {
            Name = Path.GetFileNameWithoutExtension(FilePath);
            FullName = file?.Name ?? FilePath;
        }
        NameAndVersion = JoinNameAndVersion(Name, Version);
        if (file != null)
            DetectedTargetFramework = TryDetectTargetFramework(file);
        TargetFramework = PickTargetFramework(file);
        Resolver = new AssemblyResolver(list, FilePath, TargetFramework, place?.Container.Package);
        _typeSystem = new Lazy<DecompilerTypeSystem>(() => new DecompilerTypeSystem(File, Resolver, Settings.Create()));
        _debugSymbols = new Lazy<DebugSymbols?>(() => LoadDebugSymbols(place));
        _canGeneratePdb = new Lazy<bool>(() => AssemblyExport.CanGeneratePdb(this));
        _referenceUsage = new Lazy<ReferenceUsage>(() => new ReferenceUsage(File.Metadata));
        _root = new Lazy<BrowseNode>(CreateRoot);
    }

    /// <summary>The symbols of the PDB the list picked for this assembly, else those it finds itself.</summary>
    private DebugSymbols? LoadDebugSymbols(PackagePlace? place)
    {
        if (PEFile == null)
            return null;
        var picked = List.Pdbs.Find(FilePath);
        if (picked == null)
            return DebugSymbols.Load(PEFile, place?.Container.Package);
        return DebugSymbols.LoadPicked(picked);
    }

    /// <summary>The framework the list picked for this assembly, else the one it was built for.</summary>
    private string? PickTargetFramework(MetadataFile? file)
    {
        if (file == null)
            return null;
        var picked = List.TargetFrameworks.Find(FilePath);
        if (picked == null)
            return DetectedTargetFramework;
        IsTargetFrameworkPicked = true;
        return picked;
    }

    private BrowseNode CreateRoot() => this switch
    {
        FailedAssembly => new FailedAssemblyNode(this),
        LoadedPackage package => package.PackageRoot,
        LoadedModule => new AssemblyNode(this, null),
        _ => throw new UnreachableException(),
    };

    /// <summary>The absolute path of the file this was loaded from; for an assembly read out of a
    /// package, the path through the package, as <see cref="AssemblyPackage.PathOf"/> gives it.</summary>
    public string FilePath { get; }

    public bool IsPackage => this is LoadedPackage;

    /// <summary>The package this assembly was read out of, and its file there; null for a file on disk.</summary>
    public PackagePlace? InPackage { get; }

    /// <summary>The node <paramref name="path"/> leads to from this file's root, when this is the item
    /// of the list the path starts at; else, or when nothing is there, null. May load the nodes on the
    /// way: call it off the UI thread.</summary>
    public BrowseNode? FindNode(NodePath path)
    {
        if (!string.Equals(path.FilePath, FilePath, StringComparison.OrdinalIgnoreCase))
            return null;
        return path.Resolve(Root);
    }

    /// <summary>The item of the list that holds this assembly: itself, or the package it was read out of.</summary>
    public LoadedAssembly ListedItem
    {
        get
        {
            if (InPackage == null)
                return this;
            return InPackage.Container;
        }
    }

    /// <summary>The simple name, e.g. <c>System.Private.CoreLib</c>.</summary>
    public string Name { get; }

    /// <summary>The full display name, with version, culture and public key token.</summary>
    public string FullName { get; }

    public Version? Version { get; }

    /// <summary>The target framework the assembly was built for, e.g. <c>.NETCoreApp,Version=v10.0</c>,
    /// or null if it does not say.</summary>
    public string? DetectedTargetFramework { get; }

    /// <summary>The framework the assembly's references are resolved against: the one the list picked
    /// for it, else <see cref="DetectedTargetFramework"/>.</summary>
    public string? TargetFramework { get; }

    /// <summary>Whether the list picked <see cref="TargetFramework"/> for this assembly.</summary>
    public bool IsTargetFrameworkPicked { get; private set; }

    /// <summary><see cref="TargetFramework"/> as ILSpy shows it, e.g. <c>.NETCoreApp, v10.0</c>.</summary>
    public string? FrameworkName
    {
        get
        {
            if (TargetFramework == null)
                return null;
            return FrameworkIds.ReadableName(TargetFramework);
        }
    }

    /// <summary><see cref="TargetFramework"/> as a moniker, e.g. <c>net10.0</c>, <c>netstandard2.0</c>
    /// or <c>net48</c>.</summary>
    public string? FrameworkMoniker
    {
        get
        {
            if (TargetFramework == null)
                return null;
            return FrameworkIds.MonikerOf(TargetFramework);
        }
    }

    /// <summary>The simple name with the version, e.g. <c>System.Runtime (10.0.0.0)</c>.</summary>
    public string NameAndVersion { get; }

    private static string JoinNameAndVersion(string name, Version? version)
    {
        if (version == null)
            return name;
        return $"{name} ({version})";
    }

    /// <summary>Why the file could not be read as a .NET assembly, or null when it was. A file that
    /// failed has no metadata: only its <see cref="FilePath"/>, <see cref="Name"/> and <see cref="Root"/>
    /// tell anything about it.</summary>
    public string? LoadError => _loadError?.Message;

    /// <summary>What went wrong reading the file, in full; null when it was read.</summary>
    public string? LoadErrorDetails => _loadError?.InnerException?.ToString();

    public bool HasLoadError => this is FailedAssembly;

    /// <summary>The tree's root for this assembly. For an assembly read out of a package, it sits in
    /// the package's tree, under the folder holding its file.</summary>
    public BrowseNode Root
    {
        get
        {
            if (InPackage == null)
                return _root.Value;
            return RootInPackage(InPackage);
        }
    }

    private BrowseNode RootInPackage(PackagePlace place)
    {
        var placed = Volatile.Read(ref _rootInPackage);
        if (placed != null)
            return placed;
        var folder = place.Container.PackageRoot.FindFolderNode(place.File.Folder);
        folder.LoadChildren();
        return PlaceInPackage(folder);
    }

    /// <summary>The root of this assembly, read out of a package, under <paramref name="folder"/>:
    /// made the first time, then kept.</summary>
    internal BrowseNode PlaceInPackage(BrowseNode folder)
    {
        var root = new AssemblyNode(this, folder);
        var placed = Interlocked.CompareExchange(ref _rootInPackage, root, null);
        if (placed != null)
            return placed;
        return root;
    }

    /// <summary>Whether a PDB can be picked for this assembly: one that loaded with PE headers, which
    /// a WebCIL module lacks.</summary>
    public bool CanPickPdb => PEFile != null;

    /// <summary>Whether the PDB at <paramref name="pdbPath"/> can be read for this assembly. Reads the file.</summary>
    public PdbCheck CheckPdb(string pdbPath)
    {
        if (PEFile == null)
            return PdbCheck.Unreadable;
        return DebugSymbols.Check(PEFile, pdbPath);
    }

    /// <summary>The name a portable PDB of this assembly is written under.</summary>
    public string PdbFileName => WholeProjectDecompiler.CleanUpFileName(Name, AssemblyExport.PdbExtension);

    /// <summary>Whether a PDB can be generated for this assembly: only one whose debug directory
    /// names a PDB, which the generated one must match.</summary>
    public bool CanGeneratePdb => _canGeneratePdb.Value;

    /// <summary>Whether a class diagram can be drawn of this assembly: it loaded, with types to read.</summary>
    public bool CanCreateDiagram => this is LoadedModule;

    /// <summary>Whether this assembly's API can be compared with another's: it loaded, with types to read.</summary>
    public bool CanCompareApi => this is LoadedModule;

    internal AssemblyList List { get; }
    internal MetadataFile File => _file ?? throw new InvalidOperationException($"'{FilePath}' is no assembly that loaded.");

    /// <summary><see cref="File"/> as a portable executable; null for a WebCIL module, which has no PE headers.</summary>
    internal PEFile? PEFile => _file as PEFile;
    internal AssemblyResolver Resolver { get; }
    internal bool IsListed { get; set; }

    internal DecompilerTypeSystem TypeSystem => _typeSystem.Value;

    /// <summary>What the assembly uses of the assemblies it references. Read the first time, then kept.</summary>
    internal ReferenceUsage ReferenceUsage => _referenceUsage.Value;

    /// <summary>Reads <see cref="ReferenceUsage"/> now, unless it is read already.</summary>
    internal void ReadReferenceUsage()
    {
        _ = _referenceUsage.Value;
    }

    /// <summary>The assembly's portable PDB, read once; null when it has none.</summary>
    internal DebugSymbols? DebugSymbols => _debugSymbols.Value;

    internal MetadataModule Module => TypeSystem.MainModule;

    /// <summary>The type system decompiling with <paramref name="settings"/> reads: <see cref="TypeSystem"/>
    /// unless they turn language features on or off that it models.</summary>
    internal DecompilerTypeSystem TypeSystemFor(DecompilerSettings settings)
    {
        var options = DecompilerTypeSystem.GetOptions(settings);
        if (options == DefaultTypeSystemOptions) return TypeSystem;
        return _typeSystems.GetOrAdd(options, o => new Lazy<DecompilerTypeSystem>(() => new DecompilerTypeSystem(File, Resolver, o))).Value;
    }

    internal void WarmUp()
    {
        try
        {
            _ = TypeSystem;
        }
        catch (Exception)
        {
        }
    }

    internal void Register(EntityHandle handle, BrowseNode node) => _nodes.TryAdd(handle, node);

    internal BrowseNode? FindNode(EntityHandle handle)
    {
        if (handle.IsNil) return null;
        if (_nodes.TryGetValue(handle, out var node)) return node;

        var parent = handle.Kind == HandleKind.TypeDefinition && File.Metadata.GetTypeDefinition((TypeDefinitionHandle)handle) is var type
                     && type.GetDeclaringType().IsNil
            ? ((AssemblyNode)Root).FindNamespace(File.Metadata.GetString(type.Namespace))
            : FindNode(OwnerOf(handle));
        if (parent is null) return null;
        _ = parent.Children;
        return _nodes.GetValueOrDefault(handle);
    }

    /// <summary>The node of the namespace, type or member <paramref name="idString"/> names, written as
    /// XML documentation ids are (<c>N:</c>, <c>T:</c>, <c>M:</c>, <c>P:</c>, <c>F:</c> or <c>E:</c>
    /// and the full name); null when this assembly defines none such, or the id cannot be read.</summary>
    internal BrowseNode? FindNodeById(string idString)
    {
        if (_file == null)
            return null;
        if (idString.StartsWith(NamespaceIdPrefix, StringComparison.Ordinal))
            return ((AssemblyNode)Root).FindNamespace(idString.Substring(NamespaceIdPrefix.Length));
        var entity = FindEntity(idString);
        if (entity == null)
            return null;
        if (!ReferenceEquals(entity.ParentModule, Module))
            return null;
        return FindNodeOrAncestor(entity.MetadataToken);
    }

    private IEntity? FindEntity(string idString)
    {
        try
        {
            return IdStringProvider.FindEntity(idString, new SimpleTypeResolveContext(Module));
        }
        catch (Exception e) when (e is ArgumentException or ReflectionNameParseException)
        {
            return null;
        }
    }

    internal BrowseNode? FindNodeOrAncestor(EntityHandle handle)
    {
        for (; !handle.IsNil; handle = OwnerOf(handle))
        {
            if (FindNode(handle) is { } node) return node;
        }
        return null;
    }

    /// <summary>The entity whose node holds the node of <paramref name="handle"/>'s: a property or
    /// event for its accessor, else the declaring type; nil for a top-level type.</summary>
    private EntityHandle OwnerOf(EntityHandle handle) =>
        Module.ResolveEntity(handle) switch
        {
            IMethod { IsAccessor: true, AccessorOwner: { } owner } => owner.MetadataToken,
            IEntity { DeclaringTypeDefinition: { } type } => type.MetadataToken,
            _ => default,
        };

    private static string? TryDetectTargetFramework(MetadataFile file)
    {
        try
        {
            var id = file.DetectTargetFrameworkId();
            return string.IsNullOrEmpty(id) ? null : id;
        }
        catch (BadImageFormatException)
        {
            return null;
        }
    }

    public override string ToString() => FullName;
}

/// <summary>An assembly or WebCIL module that was read: a file on disk, or, when <see cref="LoadedAssembly.InPackage"/>
/// says so, one read out of a package, known by the path <see cref="AssemblyPackage.PathOf"/> gives it.</summary>
public sealed class LoadedModule : LoadedAssembly
{
    internal LoadedModule(AssemblyList list, MetadataFile file, PackagePlace? place = null)
        : base(list, file.FileName, file, null, place)
    {
    }
}

/// <summary>A file of the list that could not be read as a .NET assembly, kept listed to show why.</summary>
public sealed class FailedAssembly : LoadedAssembly
{
    internal FailedAssembly(AssemblyList list, AssemblyLoadException error)
        : base(list, error.FilePath, null, error, null)
    {
    }
}

/// <summary>A package of the list, whose files are browsed as folders.</summary>
public sealed class LoadedPackage : LoadedAssembly
{
    private readonly Lazy<PackageNode> _root;
    private readonly Lazy<IReadOnlyList<LoadedModule>> _assemblies;

    internal LoadedPackage(AssemblyList list, AssemblyPackage package)
        : base(list, package.FilePath, null, null, null)
    {
        Package = package;
        _root = new Lazy<PackageNode>(() => new PackageNode(this));
        _assemblies = new Lazy<IReadOnlyList<LoadedModule>>(LoadAssemblies);
    }

    /// <summary>The package's files.</summary>
    public AssemblyPackage Package { get; }

    /// <summary>The root of the package's tree, its <see cref="LoadedAssembly.Root"/>.</summary>
    internal PackageNode PackageRoot => _root.Value;

    /// <summary>The assemblies read out of the package: every assembly file it holds that loads, in
    /// the order its tree shows them. Read the first time, then kept.</summary>
    internal IReadOnlyList<LoadedModule> Assemblies => _assemblies.Value;

    /// <summary>Reads <see cref="Assemblies"/> now, unless they are read already.</summary>
    internal void ReadAssemblies()
    {
        _ = _assemblies.Value;
    }

    private IReadOnlyList<LoadedModule> LoadAssemblies()
    {
        var assemblies = new List<LoadedModule>();
        foreach (var file in Package.ListFiles())
        {
            if (!PackageContents.IsAssemblyName(file.Name))
                continue;
            var assembly = List.TryLoadOutOf(this, file);
            if (assembly != null)
                assemblies.Add(assembly);
        }
        return assemblies;
    }

    /// <summary>The assembly of <see cref="Assemblies"/> read out of <paramref name="file"/>; null when
    /// that file did not load as one.</summary>
    internal LoadedModule? FindAssembly(PackageFile file)
    {
        var path = Package.PathOf(file);
        foreach (var assembly in Assemblies)
        {
            if (string.Equals(assembly.FilePath, path, StringComparison.OrdinalIgnoreCase))
                return assembly;
        }
        return null;
    }
}
