namespace ZGF.Gui.Immediate.Widgets;

/// <summary>What a tree row shows. Colors of 0 take the theme's colors; a null
/// <see cref="IconFamily"/> draws the icon in the default font. A nonzero
/// <see cref="IconBackground"/> draws the icon on a rounded badge of that color.</summary>
public readonly record struct TreeRow(
    string Label, string? Icon = null, uint IconColor = 0, uint LabelColor = 0, string? IconFamily = null, uint IconBackground = 0);

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
    private readonly HashSet<TNode> _ancestors;
    private readonly List<FlatRow> _scratch = new();
    private object? _roots;
    private int _rootCount;
    private object? _model;
    private bool _dirty = true;
    private int _selectedIndex = -1;
    private int _rowsVersion;
    private int _selectionMissingAt = -1;
    private int _scrollMissingAt = -1;
    private TNode? _scrollNode;
    private bool _hasScrollNode;
    private bool _selectionMoved;

    public TreeViewState(IEqualityComparer<TNode>? comparer = null)
    {
        Comparer = comparer ?? EqualityComparer<TNode>.Default;
        _expanded = new HashSet<TNode>(Comparer);
        _ancestors = new HashSet<TNode>(Comparer);
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

    /// <summary>Selects <paramref name="node"/> and, unless told otherwise, scrolls it into view.
    /// A node under a collapsed parent stays selected but hidden until it shows; collapsing the
    /// parent of a showing selection moves the selection up to the nearest node still showing,
    /// which the next <see cref="UiWidgets.TreeView{TNode}"/> reports as a selection change.</summary>
    public void Select(TNode node, bool scrollIntoView = true)
    {
        Selected = node;
        HasSelection = true;
        _selectedIndex = -1;
        _selectionMissingAt = -1;
        if (scrollIntoView)
            ScrollIntoView(node);
    }

    public void ClearSelection()
    {
        Selected = default;
        HasSelection = false;
        _selectedIndex = -1;
        _selectionMissingAt = -1;
    }

    /// <summary>Scrolls <paramref name="node"/> into view on the first frame it is showing.</summary>
    public void ScrollIntoView(TNode node)
    {
        _scrollNode = node;
        _hasScrollNode = true;
        _scrollMissingAt = -1;
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

        var shown = SelectedIndex;
        _ancestors.Clear();
        if (shown >= 0)
            CollectAncestors(shown);

        _dirty = false;
        _roots = roots;
        _rootCount = roots.Count;
        _model = model;
        ScrollIndex = -1;
        Rows.Clear();
        Append(Rows, roots, 0, model);
        RowsChanged();

        if (shown < 0) return;
        _selectedIndex = -1;
        var deepest = -1;
        for (var i = 0; i < Rows.Count; i++)
        {
            var node = Rows[i].Node;
            if (Comparer.Equals(node, Selected!))
            {
                _selectedIndex = i;
                break;
            }
            if (_ancestors.Contains(node) && (deepest < 0 || Rows[i].Depth > Rows[deepest].Depth))
                deepest = i;
        }
        _ancestors.Clear();
        if (_selectedIndex >= 0) return;

        if (deepest >= 0)
            SelectAt(deepest);
        else
            ClearSelection();
        _selectionMoved = true;
    }

    internal int SelectedIndex
    {
        get
        {
            if (!HasSelection) return -1;
            if (_selectedIndex >= 0 && _selectedIndex < Rows.Count && Comparer.Equals(Rows[_selectedIndex].Node, Selected!))
                return _selectedIndex;
            if (_selectionMissingAt == _rowsVersion) return -1;
            _selectedIndex = IndexOf(Selected!);
            if (_selectedIndex < 0) _selectionMissingAt = _rowsVersion;
            return _selectedIndex;
        }
    }

    /// <summary>True once after the selection moved or cleared because its node stopped showing.</summary>
    internal bool TakeSelectionMoved()
    {
        var moved = _selectionMoved;
        _selectionMoved = false;
        return moved;
    }

    internal void SelectAt(int index)
    {
        Selected = Rows[index].Node;
        HasSelection = true;
        _selectedIndex = index;
        _selectionMissingAt = -1;
        ScrollIndex = index;
    }

    internal int TakeScrollIndex()
    {
        if (_hasScrollNode && _scrollMissingAt != _rowsVersion)
        {
            var index = IndexOf(_scrollNode!);
            if (index >= 0)
            {
                _hasScrollNode = false;
                _scrollNode = default;
                ScrollIndex = index;
            }
            else
            {
                _scrollMissingAt = _rowsVersion;
            }
        }
        return ScrollIndex;
    }

    private void RowsChanged() => _rowsVersion++;

    private void CollectAncestors(int index)
    {
        var depth = Rows[index].Depth;
        for (var i = index - 1; i >= 0 && depth > 0; i--)
        {
            if (Rows[i].Depth >= depth) continue;
            depth = Rows[i].Depth;
            _ancestors.Add(Rows[i].Node);
        }
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
        RowsChanged();
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
        RowsChanged();

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
