namespace ZGF.Gui.Immediate.Widgets;

/// <summary>What a tree row shows. Colors of 0 take the style's colors; a null
/// <see cref="IconFamily"/> draws the icon in the default font.</summary>
public readonly record struct TreeRow(string Label, string? Icon = null, uint IconColor = 0, uint LabelColor = 0, string? IconFamily = null);

/// <summary>The app's hierarchy as <see cref="UiWidgets.TreeView{TNode}"/> reads it. Children are
/// asked for when a node is expanded, and again only after
/// <see cref="TreeViewState{TNode}.Refresh"/>; rows only for the nodes in view.</summary>
public interface ITreeModel<TNode>
{
    bool HasChildren(TNode node);
    IReadOnlyList<TNode> GetChildren(TNode node);
    TreeRow Row(TNode node);
}

/// <summary>
/// A tree view's expansion, selection and scroll requests, owned by the app. Nodes are told apart
/// by <see cref="Comparer"/>, so expansion follows a node wherever it moves. The visible rows are
/// flattened once and patched as nodes expand and collapse, so a frame costs only the rows in
/// view however large the tree.
/// </summary>
public sealed class TreeViewState<TNode> where TNode : notnull
{
    internal readonly record struct FlatRow(TNode Node, int Depth);

    private readonly HashSet<TNode> _expanded;
    private readonly List<FlatRow> _scratch = new();
    private object? _roots;
    private int _rootCount;
    private object? _model;
    private bool _dirty = true;
    private int _selectedIndex = -1;
    private TNode? _scrollNode;
    private bool _hasScrollNode;

    public TreeViewState(IEqualityComparer<TNode>? comparer = null)
    {
        Comparer = comparer ?? EqualityComparer<TNode>.Default;
        _expanded = new HashSet<TNode>(Comparer);
    }

    public IEqualityComparer<TNode> Comparer { get; }

    public bool HasSelection { get; private set; }

    /// <summary>The selected node; meaningful only while <see cref="HasSelection"/>.</summary>
    public TNode? Selected { get; private set; }

    /// <summary>How many rows are showing, as of the last frame.</summary>
    public int VisibleCount => Rows.Count;

    internal List<FlatRow> Rows { get; } = new();

    internal int ScrollIndex { get; set; } = -1;

    public bool IsExpanded(TNode node) => _expanded.Contains(node);

    public void SetExpanded(TNode node, bool expanded)
    {
        if (expanded ? _expanded.Add(node) : _expanded.Remove(node))
            _dirty = true;
    }

    public void Toggle(TNode node) => SetExpanded(node, !IsExpanded(node));

    public void CollapseAll()
    {
        _expanded.Clear();
        _dirty = true;
    }

    /// <summary>Selects <paramref name="node"/> and, unless told otherwise, scrolls it into view.</summary>
    public void Select(TNode node, bool scrollIntoView = true)
    {
        Selected = node;
        HasSelection = true;
        _selectedIndex = -1;
        if (scrollIntoView)
            ScrollIntoView(node);
    }

    public void ClearSelection()
    {
        Selected = default;
        HasSelection = false;
        _selectedIndex = -1;
    }

    /// <summary>Scrolls <paramref name="node"/> into view on the next frame it is showing.</summary>
    public void ScrollIntoView(TNode node)
    {
        _scrollNode = node;
        _hasScrollNode = true;
    }

    /// <summary>Expands every node of <paramref name="path"/>, root first, but the last, then
    /// selects the last and scrolls it into view.</summary>
    public void Reveal(IReadOnlyList<TNode> path)
    {
        if (path.Count == 0) return;
        for (var i = 0; i < path.Count - 1; i++)
            SetExpanded(path[i], true);
        Select(path[^1]);
    }

    /// <summary>Asks for children again: call after the hierarchy under expanded nodes changed.
    /// A different roots list, or a change in its count, is noticed without it.</summary>
    public void Refresh() => _dirty = true;

    internal void Sync(IReadOnlyList<TNode> roots, ITreeModel<TNode> model)
    {
        if (!_dirty && ReferenceEquals(roots, _roots) && roots.Count == _rootCount && ReferenceEquals(model, _model))
            return;

        _dirty = false;
        _roots = roots;
        _rootCount = roots.Count;
        _model = model;
        _selectedIndex = -1;
        ScrollIndex = -1;
        Rows.Clear();
        Append(Rows, roots, 0, model);
    }

    internal int SelectedIndex
    {
        get
        {
            if (!HasSelection) return -1;
            if (_selectedIndex >= 0 && _selectedIndex < Rows.Count && Comparer.Equals(Rows[_selectedIndex].Node, Selected!))
                return _selectedIndex;
            return _selectedIndex = IndexOf(Selected!);
        }
    }

    internal void SelectAt(int index)
    {
        Selected = Rows[index].Node;
        HasSelection = true;
        _selectedIndex = index;
        ScrollIndex = index;
    }

    internal int TakeScrollIndex()
    {
        if (_hasScrollNode)
        {
            _hasScrollNode = false;
            ScrollIndex = IndexOf(_scrollNode!);
            _scrollNode = default;
        }
        return ScrollIndex;
    }

    internal void Expand(int index, ITreeModel<TNode> model)
    {
        var row = Rows[index];
        _expanded.Add(row.Node);
        if (!model.HasChildren(row.Node)) return;
        _scratch.Clear();
        Append(_scratch, model.GetChildren(row.Node), row.Depth + 1, model);
        Rows.InsertRange(index + 1, _scratch);
        if (_selectedIndex > index)
            _selectedIndex += _scratch.Count;
        _scratch.Clear();
    }

    /// <summary>Returns true when the selection was inside and moved up to the collapsed node.</summary>
    internal bool Collapse(int index)
    {
        var row = Rows[index];
        _expanded.Remove(row.Node);
        var selected = SelectedIndex;
        var end = index + 1;
        while (end < Rows.Count && Rows[end].Depth > row.Depth)
            end++;
        Rows.RemoveRange(index + 1, end - index - 1);

        if (selected > index && selected < end)
        {
            SelectAt(index);
            return true;
        }
        if (selected >= end)
            _selectedIndex = selected - (end - index - 1);
        return false;
    }

    internal int ParentIndex(int index)
    {
        var depth = Rows[index].Depth;
        for (var i = index - 1; i >= 0; i--)
        {
            if (Rows[i].Depth < depth)
                return i;
        }
        return -1;
    }

    private int IndexOf(TNode node)
    {
        for (var i = 0; i < Rows.Count; i++)
        {
            if (Comparer.Equals(Rows[i].Node, node))
                return i;
        }
        return -1;
    }

    private void Append(List<FlatRow> into, IReadOnlyList<TNode> nodes, int depth, ITreeModel<TNode> model)
    {
        for (var i = 0; i < nodes.Count; i++)
        {
            var node = nodes[i];
            into.Add(new FlatRow(node, depth));
            if (_expanded.Contains(node) && model.HasChildren(node))
                Append(into, model.GetChildren(node), depth + 1, model);
        }
    }
}
