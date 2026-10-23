using ZGF.ILSpy.Core.Metadata;
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

    /// <summary>Where this node sits, for finding it again after its assembly is reloaded.</summary>
    public NodePath Path => _path ??= NodePath.Of(this);

    /// <summary>A child of kind <see cref="NodeKind.Loading"/> to show while <see cref="Children"/>
    /// are being built; the same node every time.</summary>
    public BrowseNode LoadingChild =>
        Volatile.Read(ref _loading) ?? Interlocked.CompareExchange(ref _loading, new LoadingNode(Assembly, this), null) ?? _loading!;

    private protected virtual bool MayHaveChildren => false;

    private protected virtual BrowseNode[] CreateChildren() => NoChildren;

    /// <summary>Decompiles this node. Throws <see cref="OperationCanceledException"/> when cancelled;
    /// any other failure becomes a document describing it. Safe to call concurrently.</summary>
    public DecompiledDocument Decompile(DecompilationLanguage language, CancellationToken cancellationToken = default) =>
        Decompile(language, DecompilerOptions.Default, cancellationToken);

    /// <summary>Decompiles this node with <paramref name="options"/>, which only C# reads.</summary>
    public DecompiledDocument Decompile(DecompilationLanguage language, DecompilerOptions options, CancellationToken cancellationToken = default) =>
        NodeDecompiler.Decompile(this, language, options, cancellationToken);

    /// <summary>The name a file of this node's code in <paramref name="language"/> is offered under.</summary>
    public string SuggestCodeFileName(DecompilationLanguage language) => NodeDecompiler.SuggestCodeFileName(this, language);

    /// <summary>Writes this node's code in <paramref name="language"/> to the file at
    /// <paramref name="path"/>: for an assembly its whole module, for a namespace all its types, else
    /// what the node shows. Throws when decompiling or writing fails.</summary>
    public void SaveCode(DecompilationLanguage language, DecompilerOptions options, string path, CancellationToken cancellationToken = default) =>
        NodeDecompiler.SaveCode(this, language, options, path, cancellationToken);

    /// <summary>Whether <see cref="LoadContentAsync"/> gives decompiled code, which changes with the
    /// language and options, rather than a grid or a picture, which do not.</summary>
    public bool DependsOnLanguage => this is not MetadataGridNode && !IsImage;

    private bool IsImage => Kind == NodeKind.ResourceImage && this is IResourceData;

    /// <summary>What this node shows, made on the thread pool: for metadata its grid of rows, read
    /// the first time and kept; for a resource named as a PNG, JPEG, BMP, GIF, ICO, CUR or SVG file its
    /// picture, or when that cannot be decoded the resource as a document with why first; else the
    /// node decompiled with <paramref name="options"/>, which only C# reads.</summary>
    public Task<NodeContent> LoadContentAsync(DecompilationLanguage language, DecompilerOptions options, CancellationToken cancellationToken = default) =>
        Task.Run(() => LoadContent(language, options, cancellationToken), cancellationToken);

    private NodeContent LoadContent(DecompilationLanguage language, DecompilerOptions options, CancellationToken cancellationToken)
    {
        if (this is MetadataGridNode grid) return new GridContent(grid.Grid);
        if (IsImage)
        {
            try
            {
                return new ImageContent(ResourceImage.Decode(ReadResource()));
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                return new DocumentContent(NodeDecompiler.Decompile(this, language, options, cancellationToken, e.Message));
            }
        }
        return new DocumentContent(Decompile(language, options, cancellationToken));
    }

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

    public override string ToString() => Text;
}

/// <summary>What a <see cref="BrowseNode"/> shows: a <see cref="DocumentContent"/>, a
/// <see cref="GridContent"/> or an <see cref="ImageContent"/>.</summary>
public abstract record NodeContent;

public sealed record DocumentContent(DecompiledDocument Document) : NodeContent;

public sealed record GridContent(MetadataGrid Grid) : NodeContent;

public sealed record ImageContent(ResourceImage Image) : NodeContent;

internal sealed class LoadingNode(LoadedAssembly assembly, BrowseNode parent) : BrowseNode(assembly, parent, NodeKind.Loading, "Loading…");
