using System.Collections.Concurrent;
using System.Reflection.Metadata;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp.ProjectDecompiler;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;

namespace ZGF.ILSpy.Core;

/// <summary>Where an assembly read out of a package came from: the package, as listed, and its file there.</summary>
public sealed record PackagePlace(LoadedAssembly Container, PackageFile File);

/// <summary>
/// An assembly file known to an <see cref="AssemblyList"/>: one the user opened, or one loaded to
/// resolve a reference. Its <see cref="Root"/> node, and every node under it, keep their identity
/// for as long as this object lives.
/// </summary>
public sealed class LoadedAssembly
{
    private static readonly TypeSystemOptions DefaultTypeSystemOptions = DecompilerTypeSystem.GetOptions(Settings.Create());

    private readonly Lazy<DecompilerTypeSystem> _typeSystem;
    private readonly Lazy<DebugSymbols?> _debugSymbols;
    private readonly Lazy<bool> _canGeneratePdb;
    private readonly ConcurrentDictionary<TypeSystemOptions, Lazy<DecompilerTypeSystem>> _typeSystems = new();
    private readonly ConcurrentDictionary<EntityHandle, BrowseNode> _nodes = new();
    private readonly Lazy<BrowseNode> _root;
    private readonly Lazy<IReadOnlyList<LoadedAssembly>> _packageAssemblies;
    private readonly MetadataFile? _file;
    private readonly AssemblyLoadException? _loadError;
    private BrowseNode? _rootInPackage;

    internal LoadedAssembly(AssemblyList list, MetadataFile file) : this(list, file.FileName, file, null, null, null)
    {
    }

    /// <summary>A file of the list that could not be read as a .NET assembly, kept listed to show why.</summary>
    internal LoadedAssembly(AssemblyList list, AssemblyLoadException error) : this(list, error.FilePath, null, error, null, null)
    {
    }

    /// <summary>A package of the list, whose files are browsed as folders.</summary>
    internal LoadedAssembly(AssemblyList list, AssemblyPackage package) : this(list, package.FilePath, null, null, package, null)
    {
    }

    /// <summary>An assembly read out of a package, known by the path <see cref="AssemblyPackage.PathOf"/> gives it.</summary>
    internal LoadedAssembly(AssemblyList list, MetadataFile file, PackagePlace place) : this(list, file.FileName, file, null, null, place)
    {
    }

    private LoadedAssembly(AssemblyList list, string filePath, MetadataFile? file, AssemblyLoadException? error, AssemblyPackage? package, PackagePlace? place)
    {
        List = list;
        _file = file;
        _loadError = error;
        FilePath = filePath;
        Package = package;
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
            TargetFramework = TryDetectTargetFramework(file);
        Resolver = new AssemblyResolver(list, FilePath, TargetFramework, place?.Container.Package);
        _typeSystem = new Lazy<DecompilerTypeSystem>(() => new DecompilerTypeSystem(File, Resolver, Settings.Create()));
        _debugSymbols = new Lazy<DebugSymbols?>(() => LoadDebugSymbols(place));
        _canGeneratePdb = new Lazy<bool>(() => AssemblyExport.CanGeneratePdb(this));
        _root = new Lazy<BrowseNode>(CreateRoot);
        _packageAssemblies = new Lazy<IReadOnlyList<LoadedAssembly>>(LoadPackageAssemblies);
    }

    /// <summary>The symbols of the PDB the list picked for this assembly, else those it finds itself.</summary>
    private DebugSymbols? LoadDebugSymbols(PackagePlace? place)
    {
        if (PEFile == null)
            return null;
        var picked = List.FindPickedPdb(FilePath);
        if (picked == null)
            return DebugSymbols.Load(PEFile, place?.Container.Package);
        return DebugSymbols.LoadPicked(picked);
    }

    private BrowseNode CreateRoot()
    {
        if (_loadError != null)
            return new FailedAssemblyNode(this);
        if (Package != null)
            return new PackageNode(this, Package);
        return new AssemblyNode(this, null);
    }

    /// <summary>The absolute path of the file this was loaded from; for an assembly read out of a
    /// package, the path through the package, as <see cref="AssemblyPackage.PathOf"/> gives it.</summary>
    public string FilePath { get; }

    /// <summary>The files of a package, when this is one; null for an assembly.</summary>
    public AssemblyPackage? Package { get; }

    public bool IsPackage => Package != null;

    /// <summary>The package this assembly was read out of, and its file there; null for a file on disk.</summary>
    public PackagePlace? InPackage { get; }

    /// <summary>For a package, the assemblies read out of it: every assembly file it holds that loads,
    /// in the order its tree shows them. None for an assembly. Read the first time, then kept.</summary>
    internal IReadOnlyList<LoadedAssembly> PackageAssemblies => _packageAssemblies.Value;

    /// <summary>Reads <see cref="PackageAssemblies"/> now, unless they are read already; does nothing
    /// for an assembly.</summary>
    internal void ReadPackageAssemblies()
    {
        _ = _packageAssemblies.Value;
    }

    private IReadOnlyList<LoadedAssembly> LoadPackageAssemblies()
    {
        var assemblies = new List<LoadedAssembly>();
        if (Package == null)
            return assemblies;
        foreach (var file in Package.ListFiles())
        {
            if (!PackageContents.IsAssemblyName(file.Name))
                continue;
            var assembly = List.TryLoad(Package.PathOf(file));
            if (assembly != null)
                assemblies.Add(assembly);
        }
        return assemblies;
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
    public string? TargetFramework { get; }

    /// <summary><see cref="TargetFramework"/> as ILSpy shows it, e.g. <c>.NETCoreApp, v10.0</c>.</summary>
    public string? FrameworkName => TargetFramework?.Replace(",Version=", ", ");

    /// <summary><see cref="TargetFramework"/> as a moniker, e.g. <c>net10.0</c>, <c>netstandard2.0</c>
    /// or <c>net48</c>.</summary>
    public string? FrameworkMoniker
    {
        get
        {
            if (TargetFramework is not { } id) return null;
            var at = id.IndexOf(",Version=v", StringComparison.Ordinal);
            if (at < 0) return id;
            var name = id[..at];
            var version = id[(at + ",Version=v".Length)..];
            return name switch
            {
                ".NETCoreApp" => System.Version.TryParse(version, out var v) && v.Major >= 5 ? $"net{version}" : $"netcoreapp{version}",
                ".NETStandard" => $"netstandard{version}",
                ".NETFramework" => $"net{version.Replace(".", "")}",
                _ => $"{name} {version}",
            };
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

    public bool HasLoadError => _loadError != null;

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
        var packageRoot = (PackageNode)place.Container.Root;
        var folder = packageRoot.FindFolderNode(place.File.Folder);
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

    internal AssemblyList List { get; }
    internal MetadataFile File => _file ?? throw new InvalidOperationException($"'{FilePath}' is no assembly that loaded.");

    /// <summary><see cref="File"/> as a portable executable; null for a WebCIL module, which has no PE headers.</summary>
    internal PEFile? PEFile => _file as PEFile;
    internal AssemblyResolver Resolver { get; }
    internal bool IsListed { get; set; }

    internal DecompilerTypeSystem TypeSystem => _typeSystem.Value;

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
