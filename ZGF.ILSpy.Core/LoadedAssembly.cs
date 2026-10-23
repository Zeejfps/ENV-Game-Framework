using System.Collections.Concurrent;
using System.Reflection.Metadata;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp.ProjectDecompiler;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;

namespace ZGF.ILSpy.Core;

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
    private readonly Lazy<AssemblyNode> _root;

    internal LoadedAssembly(AssemblyList list, PEFile file)
    {
        List = list;
        File = file;
        FilePath = file.FileName;
        var metadata = file.Metadata;
        if (metadata.IsAssembly)
        {
            var definition = metadata.GetAssemblyDefinition();
            Name = metadata.GetString(definition.Name);
            Version = definition.Version;
            FullName = file.FullName;
        }
        else
        {
            Name = Path.GetFileNameWithoutExtension(FilePath);
            FullName = file.Name;
        }
        TargetFramework = TryDetectTargetFramework(file);
        Resolver = new AssemblyResolver(list, FilePath, TargetFramework);
        _typeSystem = new Lazy<DecompilerTypeSystem>(() => new DecompilerTypeSystem(File, Resolver, Settings.Create()));
        _debugSymbols = new Lazy<DebugSymbols?>(() => DebugSymbols.Load(File));
        _canGeneratePdb = new Lazy<bool>(() => AssemblyExport.CanGeneratePdb(this));
        _root = new Lazy<AssemblyNode>(() => new AssemblyNode(this));
    }

    /// <summary>The absolute path of the file this was loaded from.</summary>
    public string FilePath { get; }

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
    public string NameAndVersion => Version is { } version ? $"{Name} ({version})" : Name;

    /// <summary>The tree's root for this assembly.</summary>
    public BrowseNode Root => _root.Value;

    /// <summary>The name a project of this assembly is offered under.</summary>
    public string ProjectFileName => WholeProjectDecompiler.CleanUpFileName(Name, AssemblyExport.ProjectExtension);

    /// <summary>The name a portable PDB of this assembly is written under.</summary>
    public string PdbFileName => WholeProjectDecompiler.CleanUpFileName(Name, AssemblyExport.PdbExtension);

    /// <summary>Whether a PDB can be generated for this assembly: only one whose debug directory
    /// names a PDB, which the generated one must match.</summary>
    public bool CanGeneratePdb => _canGeneratePdb.Value;

    internal AssemblyList List { get; }
    internal PEFile File { get; }
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

    private static string? TryDetectTargetFramework(PEFile file)
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
