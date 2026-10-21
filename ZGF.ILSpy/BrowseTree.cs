using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;

namespace ZGF.ILSpy;

/// <summary>The browse tree over the open assemblies: its roots, the model the tree view reads
/// and the view's expansion and selection.</summary>
public sealed class BrowseTree : ITreeModel<BrowseNode>
{
    private readonly Dictionary<BrowseNode, IReadOnlyList<BrowseNode>> _publicChildren = new(ReferenceEqualityComparer.Instance);
    private BrowseNode[] _roots = [];
    private int _rootsVersion = -1;
    private bool _showNonPublic = true;
    private UiStyle _style = UiStyle.Dark;
    private bool _dark = true;

    public TreeViewState<BrowseNode> View { get; } = new(ReferenceEqualityComparer.Instance);

    public IReadOnlyList<BrowseNode> Roots => _roots;

    /// <summary>Rebuilds the roots when the assembly list changed. Returns true when it did.</summary>
    public bool Sync(AssemblyList assemblies)
    {
        var version = assemblies.Version;
        if (version == _rootsVersion) return false;
        _rootsVersion = version;
        var listed = assemblies.Assemblies;
        var roots = new BrowseNode[listed.Count];
        for (var i = 0; i < roots.Length; i++)
            roots[i] = listed[i].Root;
        _roots = roots;
        return true;
    }

    public void SetShowNonPublic(bool show)
    {
        if (show == _showNonPublic) return;
        _showNonPublic = show;
        _publicChildren.Clear();
        View.Refresh();
    }

    public void SetStyle(UiStyle style, bool dark)
    {
        _style = style;
        _dark = dark;
    }

    /// <summary>Expands the tree down to <paramref name="node"/>, selects it and scrolls to it.</summary>
    public void Reveal(BrowseNode node) => View.Reveal(PathTo(node));

    public static IReadOnlyList<BrowseNode> PathTo(BrowseNode node)
    {
        var path = new List<BrowseNode>();
        for (var n = node; n != null; n = n.Parent)
            path.Add(n);
        path.Reverse();
        return path;
    }

    public bool HasChildren(BrowseNode node) => node.HasChildren;

    public IReadOnlyList<BrowseNode> GetChildren(BrowseNode node)
    {
        if (_showNonPublic) return node.Children;
        if (_publicChildren.TryGetValue(node, out var cached)) return cached;
        var visible = node.Children.Where(static c => c.IsPublicApi).ToArray();
        _publicChildren[node] = visible;
        return visible;
    }

    public TreeRow Row(BrowseNode node) => new(
        node.Text,
        NodeIcons.Glyph(node.Kind),
        NodeIcons.Color(node.Kind, _dark),
        node.IsPublicApi ? 0u : _style.TextMuted,
        NodeIcons.Family);
}
