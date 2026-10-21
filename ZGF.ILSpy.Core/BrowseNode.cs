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

    public BrowseNode? Parent { get; }

    public LoadedAssembly Assembly { get; }

    /// <summary>The entity this node defines, for type and member nodes; null otherwise.</summary>
    public CodeReference? Reference { get; private protected init; }

    /// <summary>Where activating this node should navigate, for nodes that point elsewhere (a base type,
    /// an assembly reference); null otherwise.</summary>
    public CodeReference? Target { get; private protected init; }

    /// <summary>A qualified name for copying, e.g. <c>System.Collections.Generic.List&lt;T&gt;.Add(T)</c>.</summary>
    public virtual string FullName => Text;

    /// <summary>Whether this node has children, answered without building them where possible.</summary>
    public bool HasChildren => Volatile.Read(ref _children) is { } children ? children.Length > 0 : MayHaveChildren;

    /// <summary>The children in ILSpy's order, built on first access.</summary>
    public IReadOnlyList<BrowseNode> Children
    {
        get
        {
            if (Volatile.Read(ref _children) is { } children) return children;
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

    private protected virtual bool MayHaveChildren => false;

    private protected virtual BrowseNode[] CreateChildren() => NoChildren;

    /// <summary>Decompiles this node. Throws <see cref="OperationCanceledException"/> when cancelled;
    /// any other failure becomes a document describing it. Safe to call concurrently.</summary>
    public DecompiledDocument Decompile(DecompilationLanguage language, CancellationToken cancellationToken = default) =>
        NodeDecompiler.Decompile(this, language, cancellationToken);

    /// <summary><see cref="Decompile"/> on the thread pool.</summary>
    public Task<DecompiledDocument> DecompileAsync(DecompilationLanguage language, CancellationToken cancellationToken = default) =>
        Task.Run(() => Decompile(language, cancellationToken), cancellationToken);

    public override string ToString() => Text;
}
