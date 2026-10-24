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
    private readonly ConditionalWeakTable<BrowseNode, BrowseNode[]> _publicChildren = new();
    private readonly HashSet<BrowseNode> _building = new(ReferenceEqualityComparer.Instance);
    private BrowseNode[] _roots = [];
    private int _rootsVersion = -1;
    private readonly UserIntent _expansion = new();
    private bool _showNonPublic = true;
    private Dictionary<BrowseNode, RootLabel> _rootLabels = new(ReferenceEqualityComparer.Instance);
    private UiTheme _theme = UiTheme.Dark;
    private NodeIconPalette _icons = NodeIconPalette.Dark;
    private TokenFormat _tokens = TokenFormat.Hidden;

    public TreeViewState<BrowseNode> View { get; } = new(ReferenceEqualityComparer.Instance);
    public IReadOnlyList<BrowseNode> Roots => _roots;

    /// <summary>Rebuilds the roots when the assembly list changed. Returns true when it did.</summary>
    public bool Sync(AssemblyList assemblies)
    {
        var version = assemblies.Version;
        if (version == _rootsVersion) return false;
        _rootsVersion = version;
        var listed = assemblies.Listed;
        var roots = new BrowseNode[listed.Count];
        for (var i = 0; i < roots.Length; i++)
            roots[i] = listed[i].Root;
        _roots = roots;
        _rootLabels = RootLabels(roots);
        return true;
    }

    /// <summary>The node's name as tabs show it: assemblies listed more than once under one name
    /// also show their version, or their folder when that is the same too.</summary>
    public string Label(BrowseNode node) =>
        node.Kind == NodeKind.Assembly && _rootLabels.TryGetValue(node, out var label) ? label.Title : NodeText.Name(node);

    /// <summary>The node's name as its row shows it, beside <see cref="NodeText.Secondary"/>: an
    /// assembly's folder is added only when the version and framework there do not tell it apart.</summary>
    public string RowLabel(BrowseNode node) =>
        node.Kind == NodeKind.Assembly && _rootLabels.TryGetValue(node, out var label) ? label.Row : NodeText.Name(node);

    public void SetShowNonPublic(bool show)
    {
        if (show == _showNonPublic) return;
        _showNonPublic = show;
        _publicChildren.Clear();
        View.Refresh();
    }

    public void SetStyle(UiTheme theme, NodeIconPalette icons)
    {
        _theme = theme;
        _icons = icons;
    }

    /// <summary>Writes each type's and member's metadata token after its name as <paramref name="format"/> says.</summary>
    public void ShowTokens(TokenFormat format) => _tokens = format;

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

    /// <summary>Expands every assembly and the namespaces in it, building an assembly's children in the
    /// background first when they are not built yet. Collapsing before they are built keeps it collapsed.</summary>
    public void ExpandNamespaces()
    {
        var ticket = _expansion.Begin();
        foreach (var root in _roots)
            ExpandAssembly(root, ticket);
        View.Refresh();
    }

    private async void ExpandAssembly(BrowseNode root, int ticket)
    {
        View.SetExpanded(root, true);
        var built = await Background.Run(() => BuildChildren(root));
        if (_expansion.IsStale(ticket))
            return;
        var children = built.Value;
        if (children == null)
            return;
        foreach (var child in children)
            ExpandIfNamespace(child);
        View.Refresh();
    }

    private static IReadOnlyList<BrowseNode> BuildChildren(BrowseNode node)
    {
        node.LoadChildren();
        return node.Children;
    }

    private void ExpandIfNamespace(BrowseNode node)
    {
        if (node.Kind == NodeKind.Namespace)
            View.SetExpanded(node, true);
    }

    public void CollapseAll()
    {
        _expansion.Begin();
        View.CollapseAll();
    }

    /// <summary>Collapses everything and selects nothing, as when the nodes it showed are gone.</summary>
    public void Forget()
    {
        View.ClearSelection();
        CollapseAll();
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
        RowLabel(node),
        NodeIcons.Icon(node.Kind, node.Accessibility, _icons),
        node.IsPublicApi && node.Kind != NodeKind.Loading ? 0u : _theme.TextMuted,
        Tooltip: node.Kind == NodeKind.Loading ? null : NodeText.Tooltip(node),
        Secondary: SecondaryOf(node));

    /// <summary>The node's metadata token when the tree shows tokens and it has one, else what
    /// <see cref="NodeText.Secondary"/> adds.</summary>
    private string? SecondaryOf(BrowseNode node)
    {
        var token = NodeText.Token(node, _tokens);
        if (token != null)
            return token;
        return NodeText.Secondary(node);
    }

    private readonly record struct RootLabel(string Title, string Row);

    private static Dictionary<BrowseNode, RootLabel> RootLabels(BrowseNode[] roots)
    {
        var labels = new Dictionary<BrowseNode, RootLabel>(ReferenceEqualityComparer.Instance);
        var byName = roots
            .Where(static r => r.Kind == NodeKind.Assembly)
            .GroupBy(static r => r.Assembly.Name, StringComparer.OrdinalIgnoreCase);
        foreach (var group in byName)
        {
            var nodes = group.ToList();
            if (nodes.Count == 1)
            {
                labels[nodes[0]] = new RootLabel(nodes[0].Assembly.Name, nodes[0].Assembly.Name);
                continue;
            }
            var versions = nodes.Select(static n => n.Assembly.Version?.ToString() ?? "").ToArray();
            if (versions.Distinct().Count() == nodes.Count)
            {
                for (var i = 0; i < nodes.Count; i++)
                    labels[nodes[i]] = new RootLabel(nodes[i].Assembly.NameAndVersion, nodes[i].Assembly.Name);
                continue;
            }
            var hints = FolderHints(nodes);
            for (var i = 0; i < nodes.Count; i++)
            {
                var label = $"{nodes[i].Assembly.Name} ({hints[i]})";
                labels[nodes[i]] = new RootLabel(label, label);
            }
        }
        return labels;
    }

    private static string[] FolderHints(List<BrowseNode> nodes)
    {
        var folders = nodes.Select(static n => (Path.GetDirectoryName(n.Assembly.FilePath) ?? "").Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries)).ToArray();
        var deepest = folders.Max(static f => f.Length);
        for (var depth = 1; depth <= deepest; depth++)
        {
            var hints = folders.Select(f => string.Join(Path.DirectorySeparatorChar, f[Math.Max(0, f.Length - depth)..])).ToArray();
            if (hints.Distinct(StringComparer.OrdinalIgnoreCase).Count() == hints.Length)
                return hints;
        }
        return nodes.Select(static n => n.Assembly.FilePath).ToArray();
    }

    private async void Build(BrowseNode node)
    {
        if (!_building.Add(node)) return;
        await Background.Run(() => node.Children);
        _building.Remove(node);
        View.Refresh();
    }
}
