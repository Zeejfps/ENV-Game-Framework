using ZGF.ILSpy.Core.Resources;

namespace ZGF.ILSpy.Core;

/// <summary>What a <see cref="BrowseNode"/> stands for; picks its icon.</summary>
public enum NodeKind
{
    Assembly,
    ReferencesFolder,
    AssemblyReference,
    ResourcesFolder,
    Resource,
    Namespace,
    Class,
    Interface,
    Struct,
    Enum,
    Delegate,
    Record,
    RecordStruct,
    BaseTypes,
    BaseType,
    Field,
    Constant,
    Property,
    Indexer,
    Event,
    Method,
    Constructor,
    Operator,
    Metadata,
    MetadataHeader,
    MetadataDirectory,
    MetadataTables,
    MetadataTable,
    MetadataHeap,
    DerivedTypes,
    DerivedType,
    ResourcesFile,
    ResourceImage,
    FailedAssembly,
    ForwardedType,
    Package,
    PackageFolder,
    Bundle,
    ReferencedTypes,
    TypeReference,
    MethodReference,
    FieldReference,
    PropertyReference,
    EventReference,
    ModuleReference,
    ImportedMethod,
}

/// <summary>The declared accessibility of the entity a <see cref="BrowseNode"/> stands for.</summary>
public enum NodeAccessibility
{
    None,
    Public,
    Protected,
    ProtectedInternal,
    Internal,
    PrivateProtected,
    Private,
}

/// <summary>
/// A node of the browse tree: what it stands for and where it sits. Its children are built and kept by
/// a <see cref="BrowseSession"/>, so the same entity is the same node object for as long as its
/// <see cref="Assembly"/> lives. All members are thread-safe.
/// </summary>
public sealed partial class BrowseNode
{
    private NodePath? _path;

    public NodeKind Kind { get; }

    /// <summary>The label: a name, or for members an ILSpy-style signature such as <c>Add(T) : void</c>.</summary>
    public string Text { get; }

    public NodeAccessibility Accessibility { get; private init; }

    /// <summary>Whether this is visible outside its assembly, taking enclosing types into account.
    /// Non-public nodes are usually shown dimmed.</summary>
    public bool IsPublicApi { get; private init; } = true;

    /// <summary>Whether the compiler made this up rather than the source declaring it, such as a
    /// lambda's closure class or a property's backing field, or it sits inside something the
    /// compiler made up. Such nodes are usually hidden.</summary>
    public bool IsCompilerGenerated { get; private init; }

    /// <summary>Whether this is a metadata table with no rows, which the tree usually hides.</summary>
    public bool IsEmptyTable { get; private init; }

    /// <summary>Whether this is a namespace the assembly puts no types in, listed only because others
    /// nest in it, which the tree shows only when it nests namespaces.</summary>
    public bool IsEnclosingOnly { get; private init; }

    /// <summary>For a namespace, the one whose name it extends by a segment, which holds it when the
    /// tree nests namespaces; null for an outermost namespace and for any other node.</summary>
    public BrowseNode? EnclosingNamespace { get; private init; }

    public BrowseNode? Parent { get; }

    /// <summary>The root of the tree the node is in: the listed assembly, package or file it was found
    /// under, whose <see cref="Assembly"/> differs from the node's under a reference's references.</summary>
    public BrowseNode Root
    {
        get
        {
            var root = this;
            while (root.Parent != null)
                root = root.Parent;
            return root;
        }
    }

    /// <summary>The assembly the node is read from.</summary>
    public LoadedAssembly Assembly { get; }

    /// <summary>The namespace of a type used from a referenced assembly; null for other nodes.</summary>
    public string? Namespace { get; private init; }

    /// <summary>The type declaring a method imported from a module, listed under that module; null for other nodes.</summary>
    public string? DeclaringTypeName { get; private init; }

    /// <summary>The name of the assembly a forwarded type is forwarded to; null for other nodes.</summary>
    public string? ForwardedTo { get; private init; }

    /// <summary>Where this node sits, for finding it again after its assembly is reloaded.</summary>
    public NodePath Path => _path ??= NodePath.Of(this);

    /// <summary>Whether this node stands for a resource's bytes, which <see cref="ReadResource"/>
    /// reads, to save them.</summary>
    public bool HasResource => ResourceData != null;

    /// <summary>The bytes of the resource this node stands for, as a saved copy holds them: for
    /// BAML, the XAML it was compiled from.</summary>
    public byte[] ReadResource() => AsResource().ReadData();

    /// <summary>The name a saved copy of the resource this node stands for is offered under.</summary>
    public string ResourceFileName => AsResource().FileName;

    private IResourceData AsResource() =>
        ResourceData ?? throw new InvalidOperationException($"{Text} is no resource.");

    public override string ToString() => Text;
}
