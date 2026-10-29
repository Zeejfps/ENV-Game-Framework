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
    private protected LoadedAssembly(AssemblyList list, string filePath, string name, string fullName, Version? version)
    {
        List = list;
        FilePath = filePath;
        Name = name;
        FullName = fullName;
        Version = version;
        NameAndVersion = JoinNameAndVersion(name, version);
    }

    /// <summary>The name of a file that holds no assembly's name: the file's, without its extension.</summary>
    private protected static string NameOf(string filePath) => Path.GetFileNameWithoutExtension(filePath);

    /// <summary>The absolute path of the file this was loaded from; for an assembly read out of a
    /// package, the path through the package, as <see cref="AssemblyPackage.PathOf"/> gives it.</summary>
    public string FilePath { get; }

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
            if (this is LoadedModule module && module.Place != null)
                return module.Place.Container;
            return this;
        }
    }

    /// <summary>The simple name, e.g. <c>System.Private.CoreLib</c>.</summary>
    public string Name { get; }

    /// <summary>The full display name, with version, culture and public key token.</summary>
    public string FullName { get; }

    public Version? Version { get; }

    /// <summary>The simple name with the version, e.g. <c>System.Runtime (10.0.0.0)</c>.</summary>
    public string NameAndVersion { get; }

    private static string JoinNameAndVersion(string name, Version? version)
    {
        if (version == null)
            return name;
        return $"{name} ({version})";
    }

    /// <summary>The tree's root for this assembly. For an assembly read out of a package, it sits in
    /// the package's tree, under the folder holding its file.</summary>
    public BrowseNode Root => this switch
    {
        FailedAssembly failed => failed.FailedRoot,
        LoadedPackage package => package.PackageRoot,
        LoadedModule module => module.AssemblyRoot,
        _ => throw new UnreachableException(),
    };

    internal AssemblyList List { get; }
    internal bool IsListed { get; set; }

    public override string ToString() => FullName;
}

/// <summary>An assembly or WebCIL module that was read: a file on disk, or, when <see cref="Place"/>
/// says so, one read out of a package, known by the path <see cref="AssemblyPackage.PathOf"/> gives it.</summary>
public sealed class LoadedModule : LoadedAssembly
{
    private static readonly TypeSystemOptions DefaultTypeSystemOptions = DecompilerTypeSystem.GetOptions(Settings.Create());
    private const string NamespaceIdPrefix = "N:";

    private readonly Lazy<DecompilerTypeSystem> _typeSystem;
    private readonly Lazy<DebugSymbols?> _debugSymbols;
    private readonly Lazy<bool> _canGeneratePdb;
    private readonly Lazy<ReferenceUsage> _referenceUsage;
    private readonly ConcurrentDictionary<TypeSystemOptions, Lazy<DecompilerTypeSystem>> _typeSystems = new();
    private readonly ConcurrentDictionary<EntityHandle, BrowseNode> _nodes = new();
    private readonly Lazy<AssemblyNode> _root;
    private AssemblyNode? _rootInPackage;

    internal LoadedModule(AssemblyList list, MetadataFile file, PackagePlace? place = null)
        : base(list, file.FileName, NameOf(file), FullNameOf(file), VersionOf(file))
    {
        File = file;
        Place = place;
        DetectedTargetFramework = TryDetectTargetFramework(file);
        TargetFramework = PickTargetFramework();
        Resolver = new AssemblyResolver(list, FilePath, TargetFramework, place?.Container.Package);
        _typeSystem = new Lazy<DecompilerTypeSystem>(() => new DecompilerTypeSystem(File, Resolver, Settings.Create()));
        _debugSymbols = new Lazy<DebugSymbols?>(LoadDebugSymbols);
        _canGeneratePdb = new Lazy<bool>(() => AssemblyExport.CanGeneratePdb(this));
        _referenceUsage = new Lazy<ReferenceUsage>(() => new ReferenceUsage(File.Metadata));
        _root = new Lazy<AssemblyNode>(() => new AssemblyNode(this, null));
    }

    /// <summary>The assembly's name, or for a module that is no assembly, its file's.</summary>
    private static string NameOf(MetadataFile file)
    {
        if (!file.Metadata.IsAssembly)
            return NameOf(file.FileName);
        var metadata = file.Metadata;
        return metadata.GetString(metadata.GetAssemblyDefinition().Name);
    }

    private static string FullNameOf(MetadataFile file)
    {
        if (!file.Metadata.IsAssembly)
            return file.Name;
        return file.FullName;
    }

    private static Version? VersionOf(MetadataFile file)
    {
        if (!file.Metadata.IsAssembly)
            return null;
        return file.Metadata.GetAssemblyDefinition().Version;
    }

    /// <summary>The package this was read out of, and its file there; null for a file on disk.</summary>
    public PackagePlace? Place { get; }

    internal MetadataFile File { get; }

    /// <summary><see cref="File"/> as a portable executable; null for a WebCIL module, which has no PE headers.</summary>
    internal PEFile? PEFile => File as PEFile;

    internal AssemblyResolver Resolver { get; }

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

    /// <summary>The framework the list picked for this assembly, else the one it was built for.</summary>
    private string? PickTargetFramework()
    {
        var picked = List.TargetFrameworks.Find(FilePath);
        if (picked == null)
            return DetectedTargetFramework;
        IsTargetFrameworkPicked = true;
        return picked;
    }

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

    /// <summary>The root of this assembly's tree; for one read out of a package, under the folder of
    /// the package's tree holding its file.</summary>
    internal AssemblyNode AssemblyRoot
    {
        get
        {
            if (Place == null)
                return _root.Value;
            return RootInPackage(Place);
        }
    }

    private AssemblyNode RootInPackage(PackagePlace place)
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
    internal AssemblyNode PlaceInPackage(BrowseNode folder)
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

    /// <summary>The symbols of the PDB the list picked for this assembly, else those it finds itself.</summary>
    private DebugSymbols? LoadDebugSymbols()
    {
        if (PEFile == null)
            return null;
        var picked = List.Pdbs.Find(FilePath);
        if (picked == null)
            return DebugSymbols.Load(PEFile, Place?.Container.Package);
        return DebugSymbols.LoadPicked(picked);
    }

    /// <summary>The assembly's portable PDB, read once; null when it has none.</summary>
    internal DebugSymbols? DebugSymbols => _debugSymbols.Value;

    internal DecompilerTypeSystem TypeSystem => _typeSystem.Value;

    /// <summary>The module of <see cref="TypeSystem"/> this assembly's metadata is read into.</summary>
    internal MetadataModule MainModule => TypeSystem.MainModule;

    /// <summary>The type system decompiling with <paramref name="settings"/> reads: <see cref="TypeSystem"/>
    /// unless they turn language features on or off that it models.</summary>
    internal DecompilerTypeSystem TypeSystemFor(DecompilerSettings settings)
    {
        var options = DecompilerTypeSystem.GetOptions(settings);
        if (options == DefaultTypeSystemOptions) return TypeSystem;
        return _typeSystems.GetOrAdd(options, o => new Lazy<DecompilerTypeSystem>(() => new DecompilerTypeSystem(File, Resolver, o))).Value;
    }

    /// <summary>What the assembly uses of the assemblies it references. Read the first time, then kept.</summary>
    internal ReferenceUsage ReferenceUsage => _referenceUsage.Value;

    /// <summary>Reads <see cref="ReferenceUsage"/> now, unless it is read already.</summary>
    internal void ReadReferenceUsage()
    {
        _ = _referenceUsage.Value;
    }

    /// <summary>Reads <see cref="TypeSystem"/> ahead of its first use, on a thread-pool thread. Whatever
    /// reading it throws is left for that first use, which reads it again and reports it; it is caught
    /// whole here because an exception leaving a thread-pool work item ends the process.</summary>
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
            ? AssemblyRoot.FindNamespace(File.Metadata.GetString(type.Namespace))
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
        if (idString.StartsWith(NamespaceIdPrefix, StringComparison.Ordinal))
            return AssemblyRoot.FindNamespace(idString.Substring(NamespaceIdPrefix.Length));
        var entity = FindEntity(idString);
        if (entity == null)
            return null;
        if (!ReferenceEquals(entity.ParentModule, MainModule))
            return null;
        return FindNodeOrAncestor(entity.MetadataToken);
    }

    private IEntity? FindEntity(string idString)
    {
        try
        {
            return IdStringProvider.FindEntity(idString, new SimpleTypeResolveContext(MainModule));
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
            var node = FindNode(handle);
            if (node != null) return node;
        }
        return null;
    }

    /// <summary>The entity whose node holds the node of <paramref name="handle"/>'s: a property or
    /// event for its accessor, else the declaring type; nil for a top-level type.</summary>
    private EntityHandle OwnerOf(EntityHandle handle) =>
        MainModule.ResolveEntity(handle) switch
        {
            IMethod { IsAccessor: true } method when method.AccessorOwner != null => method.AccessorOwner.MetadataToken,
            IEntity entity when entity.DeclaringTypeDefinition != null => entity.DeclaringTypeDefinition.MetadataToken,
            _ => default,
        };
}

/// <summary>A file of the list that could not be read as a .NET assembly, kept listed to show why. It
/// has no metadata: only its <see cref="LoadedAssembly.FilePath"/>, <see cref="LoadedAssembly.Name"/>
/// and <see cref="Error"/> tell anything about it.</summary>
public sealed class FailedAssembly : LoadedAssembly
{
    private readonly Lazy<FailedAssemblyNode> _root;

    internal FailedAssembly(AssemblyList list, AssemblyLoadException error)
        : base(list, error.FilePath, NameOf(error.FilePath), error.FilePath, null)
    {
        Error = error;
        _root = new Lazy<FailedAssemblyNode>(() => new FailedAssemblyNode(this));
    }

    /// <summary>Why the file could not be read.</summary>
    public AssemblyLoadException Error { get; }

    /// <summary>The root of the file's tree, its <see cref="LoadedAssembly.Root"/>.</summary>
    internal FailedAssemblyNode FailedRoot => _root.Value;
}

/// <summary>A package of the list, whose files are browsed as folders.</summary>
public sealed class LoadedPackage : LoadedAssembly
{
    private readonly Lazy<PackageNode> _root;
    private readonly Lazy<IReadOnlyList<LoadedModule>> _assemblies;

    internal LoadedPackage(AssemblyList list, AssemblyPackage package)
        : base(list, package.FilePath, NameOf(package.FilePath), package.FilePath, null)
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
