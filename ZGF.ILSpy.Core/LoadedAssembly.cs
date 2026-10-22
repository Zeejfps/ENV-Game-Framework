using System.Collections.Concurrent;
using System.Reflection.Metadata;
using ICSharpCode.Decompiler;
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

    /// <summary>The tree's root for this assembly.</summary>
    public BrowseNode Root => _root.Value;

    internal AssemblyList List { get; }
    internal PEFile File { get; }
    internal AssemblyResolver Resolver { get; }
    internal bool IsListed { get; set; }

    internal DecompilerTypeSystem TypeSystem => _typeSystem.Value;

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

        BrowseNode? parent;
        if (handle.Kind == HandleKind.TypeDefinition)
        {
            var definition = File.Metadata.GetTypeDefinition((TypeDefinitionHandle)handle);
            var declaring = definition.GetDeclaringType();
            parent = declaring.IsNil
                ? ((AssemblyNode)Root).FindNamespace(File.Metadata.GetString(definition.Namespace))
                : FindNode(declaring);
        }
        else
        {
            var entity = Module.ResolveEntity(handle);
            parent = entity switch
            {
                IMethod { IsAccessor: true, AccessorOwner: { } owner } => FindNode(owner.MetadataToken),
                IEntity { DeclaringTypeDefinition: { } type } => FindNode(type.MetadataToken),
                _ => null,
            };
        }
        if (parent is null) return null;
        _ = parent.Children;
        return _nodes.GetValueOrDefault(handle);
    }

    internal BrowseNode? FindNodeOrAncestor(EntityHandle handle)
    {
        while (!handle.IsNil)
        {
            if (FindNode(handle) is { } node) return node;
            handle = Module.ResolveEntity(handle) switch
            {
                IMethod { IsAccessor: true, AccessorOwner: { } owner } => owner.MetadataToken,
                IEntity { DeclaringTypeDefinition: { } type } => type.MetadataToken,
                _ => default,
            };
        }
        return null;
    }

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
