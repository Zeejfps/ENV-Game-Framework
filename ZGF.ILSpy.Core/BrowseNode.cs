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
    Error,
    Loading,
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
/// A node of the browse tree. Children are built on first access, once, and cached, so the same
/// entity is always the same node object while its <see cref="Assembly"/> stays loaded. All members
/// are thread-safe.
/// </summary>
public abstract class BrowseNode
{
    private static readonly BrowseNode[] NoChildren = [];

    private readonly Lock _gate = new();
    private BrowseNode[]? _children;
    private NodePath? _path;
    private BrowseNode? _loading;

    private protected BrowseNode(LoadedAssembly assembly, BrowseNode? parent, NodeKind kind, string text)
    {
        Assembly = assembly;
        Parent = parent;
        Kind = kind;
        Text = text;
    }

    public NodeKind Kind { get; }

    /// <summary>The label: a name, or for members an ILSpy-style signature such as <c>Add(T) : void</c>.</summary>
    public string Text { get; }

    public NodeAccessibility Accessibility { get; private protected init; }

    /// <summary>Whether this is visible outside its assembly, taking enclosing types into account.
    /// Non-public nodes are usually shown dimmed.</summary>
    public bool IsPublicApi { get; private protected init; } = true;

    /// <summary>Whether the compiler made this up rather than the source declaring it, such as a
    /// lambda's closure class or a property's backing field, or it sits inside something the
    /// compiler made up. Such nodes are usually hidden.</summary>
    public bool IsCompilerGenerated { get; private protected init; }

    /// <summary>Whether this is a metadata table with no rows, which the tree usually hides.</summary>
    public bool IsEmptyTable { get; internal init; }

    /// <summary>Whether this is a namespace the assembly puts no types in, listed only because others
    /// nest in it, which the tree shows only when it nests namespaces.</summary>
    public bool IsEnclosingOnly { get; private protected init; }

    /// <summary>For a namespace, the one whose name it extends by a segment, which holds it when the
    /// tree nests namespaces; null for an outermost namespace and for any other node.</summary>
    public BrowseNode? EnclosingNamespace { get; private protected init; }

    /// <summary>For a namespace, the namespaces whose names extend it by a segment, sorted by name;
    /// none for any other node.</summary>
    public virtual IReadOnlyList<BrowseNode> InnerNamespaces => NoChildren;

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

    /// <summary>The entity this node defines, for type and member nodes; null otherwise.</summary>
    public CodeReference? Reference { get; private protected init; }

    /// <summary>The metadata token of the entity this node defines, for type and member nodes; null otherwise.</summary>
    public int? Token { get; private protected init; }

    /// <summary>The namespace of a type used from a referenced assembly; null for other nodes.</summary>
    public string? Namespace { get; private protected init; }

    /// <summary>The type declaring a method imported from a module, listed under that module; null for other nodes.</summary>
    public string? DeclaringTypeName { get; private protected init; }

    /// <summary>The name of the assembly a forwarded type is forwarded to; null for other nodes.</summary>
    public string? ForwardedTo { get; private protected init; }

    /// <summary>Where activating this node should navigate, for nodes that point elsewhere (a base type,
    /// an assembly reference); null otherwise.</summary>
    public CodeReference? Target { get; private protected init; }

    /// <summary>A qualified name for copying, e.g. <c>System.Collections.Generic.List&lt;T&gt;.Add(T)</c>.</summary>
    public virtual string FullName => Text;

    /// <summary>Whether this node has children, answered without building them where possible.</summary>
    public bool HasChildren
    {
        get
        {
            var children = Volatile.Read(ref _children);
            return children != null ? children.Length > 0 : MayHaveChildren;
        }
    }

    /// <summary>The children in ILSpy's order, built on first access.</summary>
    public IReadOnlyList<BrowseNode> Children
    {
        get
        {
            var children = Volatile.Read(ref _children);
            if (children != null) return children;
            lock (_gate)
            {
                if (_children is null)
                {
                    BrowseNode[] built;
                    try
                    {
                        built = CreateChildren();
                    }
                    catch (Exception e)
                    {
                        built = [new ErrorNode(Assembly, this, e)];
                    }
                    Volatile.Write(ref _children, built.Length == 0 ? NoChildren : built);
                }
                return _children!;
            }
        }
    }

    public void LoadChildren()
    {
        _ = Children;
    }

    /// <summary>The children if they are built already; never blocks.</summary>
    public bool TryGetChildren(out IReadOnlyList<BrowseNode> children)
    {
        children = Volatile.Read(ref _children)!;
        return children != null;
    }

    /// <summary>The children as the tree lists them when it nests namespaces, if they are built
    /// already: for an assembly, its children but the namespaces nested in others; for a namespace,
    /// the namespaces nested in it and its own children, sorted together by name; else
    /// <see cref="Children"/>. Never blocks.</summary>
    public bool TryGetNestedChildren(out IReadOnlyList<BrowseNode> children)
    {
        if (!TryGetChildren(out children))
            return false;
        children = Nest(children);
        return true;
    }

    private protected virtual IReadOnlyList<BrowseNode> Nest(IReadOnlyList<BrowseNode> children) => children;

    /// <summary>The child standing where <paramref name="other"/> stands under its own parent, of its kind
    /// and text: its counterpart under this node's copy, read again, of the same file. Null when there is
    /// none, or the children are not built yet; never blocks.</summary>
    public BrowseNode? FindLoadedChildLike(BrowseNode other)
    {
        if (!TryGetChildren(out var children))
            return null;
        foreach (var child in children)
        {
            if (child.Kind == other.Kind && child.Text == other.Text)
                return child;
        }
        return null;
    }

    /// <summary>Where this node sits, for finding it again after its assembly is reloaded.</summary>
    public NodePath Path => _path ??= NodePath.Of(this);

    /// <summary>A child of kind <see cref="NodeKind.Loading"/> to show while <see cref="Children"/>
    /// are being built; the same node every time.</summary>
    public BrowseNode LoadingChild =>
        Volatile.Read(ref _loading) ?? Interlocked.CompareExchange(ref _loading, new LoadingNode(Assembly, this), null) ?? _loading!;

    private protected virtual bool MayHaveChildren => false;

    private protected virtual BrowseNode[] CreateChildren() => NoChildren;

    /// <summary>Whether this node stands for a resource's bytes, which <see cref="ReadResource"/>
    /// reads, to save them.</summary>
    public bool HasResource => this is IResourceData;

    /// <summary>The bytes of the resource this node stands for, as a saved copy holds them: for
    /// BAML, the XAML it was compiled from.</summary>
    public byte[] ReadResource() => AsResource().ReadData();

    /// <summary>The name a saved copy of the resource this node stands for is offered under.</summary>
    public string ResourceFileName => AsResource().FileName;

    private IResourceData AsResource() =>
        this as IResourceData ?? throw new InvalidOperationException($"{Text} is no resource.");

    /// <summary>The folder of a package this node lists, whose files can be extracted: the root folder
    /// for a package's own node; null for a node that lists none.</summary>
    public PackageFolderPlace? FolderInPackage
    {
        get
        {
            if (this is not IPackageFolderNode node)
                return null;
            return new PackageFolderPlace(node.Package, node.Folder);
        }
    }

    public override string ToString() => Text;
}

internal sealed class LoadingNode(LoadedAssembly assembly, BrowseNode parent) : BrowseNode(assembly, parent, NodeKind.Loading, "Loading…");
