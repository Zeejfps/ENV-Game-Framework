using System.Runtime.CompilerServices;
using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;

namespace ZGF.ILSpy;

/// <summary>The browse tree over the open assemblies: its roots, the model the tree view reads
/// and the view's expansion and selection. Children not built yet show a loading row while they
/// are built in the background.</summary>
public sealed class BrowseTree : ITreeModel<BrowseNode>
{
    private readonly Action<Action> _post;
    private readonly ConditionalWeakTable<BrowseNode, BrowseNode[]> _publicChildren = new();
    private readonly HashSet<BrowseNode> _building = new(ReferenceEqualityComparer.Instance);
    private BrowseNode[] _roots = [];
    private int _rootsVersion = -1;
    private bool _showNonPublic = true;
    private UiTheme _theme = UiTheme.Dark;
    private bool _dark = true;

    /// <param name="post">Runs an action on the UI thread.</param>
    public BrowseTree(Action<Action> post)
    {
        _post = post;
    }

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

    public void SetTheme(UiTheme theme, bool dark)
    {
        _theme = theme;
        _dark = dark;
    }

    /// <summary>Expands the tree down to <paramref name="node"/>, selects it and scrolls to it. When
    /// non-public nodes are hidden and it is one, or inside one, its nearest shown ancestor is
    /// selected instead.</summary>
    public void Reveal(BrowseNode node)
    {
        var path = new List<BrowseNode>();
        for (var n = node; n != null; n = n.Parent)
            path.Add(n);
        path.Reverse();
        if (!_showNonPublic)
        {
            var hidden = path.FindIndex(static n => !n.IsPublicApi);
            if (hidden > 0)
                path.RemoveRange(hidden, path.Count - hidden);
        }
        View.Reveal(path);
    }

    /// <summary>The paths of the expanded nodes that are showing, for expanding them again after
    /// a reload.</summary>
    public List<NodePath> ExpandedPaths()
    {
        var paths = new List<NodePath>();
        Collect(_roots);
        return paths;

        void Collect(IReadOnlyList<BrowseNode> nodes)
        {
            foreach (var node in nodes)
            {
                if (!View.IsExpanded(node)) continue;
                paths.Add(node.Path);
                if (node.TryGetChildren(out var children))
                    Collect(children);
            }
        }
    }

    /// <summary>Expands <paramref name="nodes"/>, e.g. those <see cref="ExpandedPaths"/> named before
    /// a reload.</summary>
    public void Expand(IEnumerable<BrowseNode> nodes)
    {
        foreach (var node in nodes)
            View.SetExpanded(node, true);
        View.Refresh();
    }

    public bool HasChildren(BrowseNode node) => node.HasChildren;

    public IReadOnlyList<BrowseNode> GetChildren(BrowseNode node)
    {
        if (!node.TryGetChildren(out var children))
        {
            Build(node);
            return [node.LoadingChild];
        }
        if (_showNonPublic) return children;
        if (_publicChildren.TryGetValue(node, out var cached)) return cached;
        var visible = children.Where(static c => c.IsPublicApi).ToArray();
        _publicChildren.AddOrUpdate(node, visible);
        return visible;
    }

    public TreeRow Row(BrowseNode node) => new(
        node.Text,
        NodeIcons.Glyph(node.Kind),
        NodeIcons.Color(node.Kind, _dark),
        node.IsPublicApi && node.Kind != NodeKind.Loading ? 0u : _theme.TextMuted,
        NodeIcons.Family);

    private void Build(BrowseNode node)
    {
        if (!_building.Add(node)) return;
        Task.Run(() => _ = node.Children).ContinueWith(_ => _post(() =>
        {
            _building.Remove(node);
            View.Refresh();
        }), TaskScheduler.Default);
    }
}
