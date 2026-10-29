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
    private readonly ConditionalWeakTable<BrowseNode, BrowseNode[]> _shownChildren = new();
    private readonly HashSet<BrowseNode> _building = new(ReferenceEqualityComparer.Instance);
    private BrowseNode[] _roots = [];
    private int _rootsVersion = -1;
    private readonly UserIntent _expansion = new();
    private ApiVisibility _visibility = ApiVisibility.AllButCompilerGenerated;
    private bool _showEmptyTables;
    private bool _nestNamespaces;
    private Dictionary<BrowseNode, RootLabel> _rootLabels = new(ReferenceEqualityComparer.Instance);
    private UiTheme _theme = UiTheme.Dark;
    private NodeIconPalette _iconColors = BuiltInThemes.Dark.Icons;
    private AppIcons _icons = BuiltInIconThemes.Reference.Icons;
    private TokenFormat _tokens = TokenFormat.Hidden;
    private IReadOnlyCollection<AssemblyPath> _reopening = [];

    public TreeViewState<BrowseNode> View { get; } = new(ReferenceEqualityComparer.Instance);
    public IReadOnlyList<BrowseNode> Roots => _roots;

    /// <summary>Changes whenever the roots change or namespaces start or stop nesting, which changes
    /// the paths and labels of the nodes.</summary>
    public int Revision { get; private set; }

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
        FollowGoneRoots(roots);
        _roots = roots;
        _rootLabels = RootLabels(roots);
        Revision++;
        return true;
    }

    /// <summary>Moves what was expanded and selected under each root no longer among
    /// <paramref name="roots"/> to the root of its file read again, when one is, and forgets it.</summary>
    private void FollowGoneRoots(BrowseNode[] roots)
    {
        if (_roots.Length == 0)
            return;
        var kept = new HashSet<BrowseNode>(roots, ReferenceEqualityComparer.Instance);
        foreach (var root in _roots)
        {
            if (kept.Contains(root))
                continue;
            var replacement = FindRootOfFile(roots, root.Assembly.FilePath);
            FollowSelection(root, replacement);
            FollowExpansion(root, replacement);
        }
    }

    private static BrowseNode? FindRootOfFile(BrowseNode[] roots, AssemblyPath filePath)
    {
        foreach (var root in roots)
        {
            if (root.Assembly.FilePath.Equals(filePath))
                return root;
        }
        return null;
    }

    private void FollowSelection(BrowseNode gone, BrowseNode? replacement)
    {
        if (replacement == null)
            return;
        if (!View.HasSelection)
            return;
        var selected = View.Selected!;
        if (selected.Root != gone)
            return;
        var counterpart = FindCounterpart(selected, replacement);
        if (counterpart != null)
            View.Select(counterpart, scrollIntoView: false);
    }

    /// <summary>The node standing where <paramref name="node"/> does, under <paramref name="root"/>
    /// in place of <paramref name="node"/>'s root, found through children already loaded.</summary>
    private static BrowseNode? FindCounterpart(BrowseNode node, BrowseNode root)
    {
        if (node.Parent == null)
            return root;
        var parent = FindCounterpart(node.Parent, root);
        if (parent == null)
            return null;
        return parent.FindLoadedChildLike(node);
    }

    /// <summary>Collapses <paramref name="node"/> and the nodes under it, expanding each one's
    /// counterpart under <paramref name="counterpart"/> instead.</summary>
    private void FollowExpansion(BrowseNode node, BrowseNode? counterpart)
    {
        if (!View.IsExpanded(node))
            return;
        View.SetExpanded(node, false);
        if (counterpart != null)
            View.SetExpanded(counterpart, true);
        if (!node.TryGetChildren(out var children))
            return;
        foreach (var child in children)
            FollowExpansion(child, FindChildLike(counterpart, child));
    }

    private static BrowseNode? FindChildLike(BrowseNode? parent, BrowseNode child)
    {
        if (parent == null)
            return null;
        return parent.FindLoadedChildLike(child);
    }

    /// <summary>The node's name as tabs show it: assemblies listed more than once under one name
    /// also show their version, or their folder when that is the same too.</summary>
    public string Label(BrowseNode node) =>
        node.Kind == NodeKind.Assembly && _rootLabels.TryGetValue(node, out var label) ? label.Title : NodeText.Name(node);

    /// <summary>The node's name as its row shows it, beside <see cref="NodeText.Secondary"/>: an
    /// assembly's folder is added only when the version and framework there do not tell it apart, and a
    /// namespace nested in another shows only its last segment.</summary>
    public string RowLabel(BrowseNode node)
    {
        if (IsNested(node))
            return NodeText.NestedName(node);
        return node.Kind == NodeKind.Assembly && _rootLabels.TryGetValue(node, out var label) ? label.Row : NodeText.Name(node);
    }

    private bool IsNested(BrowseNode node) => _nestNamespaces && node.EnclosingNamespace != null;

    public void ShowApi(ApiVisibility visibility)
    {
        if (visibility == _visibility) return;
        _visibility = visibility;
        _shownChildren.Clear();
        View.Refresh();
    }

    /// <summary>Lists the metadata tables with no rows among the others, or leaves them out.</summary>
    public void ShowEmptyTables(bool show)
    {
        if (show == _showEmptyTables) return;
        _showEmptyTables = show;
        _shownChildren.Clear();
        View.Refresh();
    }

    /// <summary>Nests each namespace in the one its name extends by a segment, or lists them all
    /// under their assembly, keeping the selected node in view.</summary>
    public void NestNamespaces(bool nest)
    {
        if (nest == _nestNamespaces) return;
        _nestNamespaces = nest;
        Revision++;
        _shownChildren.Clear();
        View.Refresh();
        RevealSelected();
    }

    private void RevealSelected()
    {
        if (!View.HasSelection)
            return;
        Reveal(View.Selected!);
    }

    public void SetStyle(UiTheme theme, NodeIconPalette iconColors, AppIcons icons)
    {
        _theme = theme;
        _iconColors = iconColors;
        _icons = icons;
    }

    /// <summary>Writes each type's and member's metadata token after its name as <paramref name="format"/> says.</summary>
    public void ShowTokens(TokenFormat format) => _tokens = format;

    /// <summary>Spins the roots of the assemblies read from <paramref name="files"/>, which are being reopened.</summary>
    public void ShowReopening(IReadOnlyCollection<AssemblyPath> files) => _reopening = files;

    /// <summary>Expands the tree down to <paramref name="node"/>, selects it and scrolls to it. When
    /// it is hidden, or inside a hidden node, its nearest shown ancestor is selected instead.</summary>
    public void Reveal(BrowseNode node)
    {
        var path = ShownPathTo(node);
        View.Reveal(path);
    }

    /// <summary>The rows from the root down to <paramref name="node"/>, or to its nearest shown
    /// ancestor when it is hidden or inside a hidden node.</summary>
    private List<BrowseNode> ShownPathTo(BrowseNode node)
    {
        var path = ListPathTo(node);
        var hidden = path.FindIndex(IsHidden);
        if (hidden > 0)
            path.RemoveRange(hidden, path.Count - hidden);
        return path;
    }

    /// <summary>The nodes from the root down to <paramref name="node"/>, each in the one whose row
    /// holds it, hidden ones included.</summary>
    public List<BrowseNode> ListPathTo(BrowseNode node)
    {
        var path = new List<BrowseNode>();
        for (var n = node; n != null; n = ShownParent(n))
            path.Add(n);
        path.Reverse();
        return path;
    }

    /// <summary>The node whose row holds <paramref name="node"/>'s: the namespace a namespace nests in
    /// when namespaces nest, else its parent.</summary>
    private BrowseNode? ShownParent(BrowseNode node)
    {
        if (IsNested(node))
            return node.EnclosingNamespace;
        return node.Parent;
    }

    private bool IsHidden(BrowseNode node)
    {
        if (node.IsEmptyTable) return !_showEmptyTables;
        if (node.Kind == NodeKind.Namespace) return IsNamespaceHidden(node);
        return IsApiHidden(node);
    }

    /// <summary>Whether a namespace is left out: when it holds no type that shows and, when namespaces
    /// nest, no namespace that shows nests in it.</summary>
    private bool IsNamespaceHidden(BrowseNode ns)
    {
        var ownHidden = ns.IsEnclosingOnly || IsApiHidden(ns);
        if (!ownHidden) return false;
        if (!_nestNamespaces) return true;
        return ns.InnerNamespaces.All(IsNamespaceHidden);
    }

    private bool IsApiHidden(BrowseNode node)
    {
        if (_visibility == ApiVisibility.All) return false;
        if (node.IsCompilerGenerated) return true;
        return _visibility == ApiVisibility.PublicOnly && !node.IsPublicApi;
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
        if (built is not Outcome<IReadOnlyList<BrowseNode>>.Ok { Value: var children })
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

    public bool HasChildren(BrowseNode node)
    {
        if (!_nestNamespaces) return node.HasChildren;
        return node.HasChildren || node.InnerNamespaces.Count > 0;
    }

    public IReadOnlyList<BrowseNode> GetChildren(BrowseNode node)
    {
        if (!TryGetShapedChildren(node, out var children))
        {
            Build(node);
            return [node.LoadingChild];
        }
        if (_shownChildren.TryGetValue(node, out var cached)) return cached;
        var shown = children
            .Where(c => !IsHidden(c))
            .ToArray();
        _shownChildren.AddOrUpdate(node, shown);
        return shown;
    }

    /// <summary>The children if they are built already, with namespaces nested when the tree nests them.</summary>
    private bool TryGetShapedChildren(BrowseNode node, out IReadOnlyList<BrowseNode> children)
    {
        if (_nestNamespaces)
            return node.TryGetNestedChildren(out children);
        return node.TryGetChildren(out children);
    }

    public TreeRow Row(BrowseNode node) => new(
        RowLabel(node),
        NodeIcons.Icon(node.Kind, node.Accessibility, _iconColors, _icons),
        node.IsPublicApi && node.Kind != NodeKind.Loading ? 0u : _theme.TextMuted,
        Tooltip: node.Kind == NodeKind.Loading ? null : NodeText.Tooltip(node),
        Secondary: SecondaryOf(node),
        SecondaryBold: NodeText.PickedFramework(node),
        Spinning: IsBusy(node));

    /// <summary>Whether the node stands in for children being built, or is the root of an assembly
    /// being reopened.</summary>
    private bool IsBusy(BrowseNode node)
    {
        if (node.Kind == NodeKind.Loading)
            return true;
        if (_reopening.Count == 0)
            return false;
        return node == node.Assembly.Root && _reopening.Contains(node.Assembly.FilePath);
    }

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
        var folders = nodes.Select(static n => (Path.GetDirectoryName(n.Assembly.FilePath.Text) ?? "").Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries)).ToArray();
        var deepest = folders.Max(static f => f.Length);
        for (var depth = 1; depth <= deepest; depth++)
        {
            var hints = folders.Select(f => string.Join(Path.DirectorySeparatorChar, f[Math.Max(0, f.Length - depth)..])).ToArray();
            if (hints.Distinct(StringComparer.OrdinalIgnoreCase).Count() == hints.Length)
                return hints;
        }
        return nodes.Select(static n => n.Assembly.FilePath.Text).ToArray();
    }

    private async void Build(BrowseNode node)
    {
        if (!_building.Add(node)) return;
        await Background.Run(() => node.Children);
        _building.Remove(node);
        View.Refresh();
    }
}
