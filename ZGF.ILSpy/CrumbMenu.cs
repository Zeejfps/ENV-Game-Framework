using ZGF.ILSpy.Core;

namespace ZGF.ILSpy;

/// <summary>The children of one crumb of a document's omnibar, listed under it while
/// <see cref="IsOpen"/>: those whose name holds the filter typed, the crumb after it on the path
/// selected once they are built.</summary>
public sealed class CrumbMenu
{
    private readonly List<BrowseNode> _shown = new();
    private IReadOnlyList<BrowseNode>? _children;
    private string _shownFilter = "";
    private BrowseNode? _toSelect;
    private bool _isFocusPending;

    /// <summary>Whether the list is open; cleared when it is dismissed.</summary>
    public bool IsOpen;

    /// <summary>What a child's name must hold to be listed, ignoring case.</summary>
    public string Filter = "";

    /// <summary>The document panel whose bar the list is open in.</summary>
    public string? Panel { get; private set; }

    /// <summary>The crumb whose children are listed.</summary>
    public BrowseNode? Node { get; private set; }

    public ItemSelection<BrowseNode> Selection { get; } = new();

    /// <summary>The children listed, as <see cref="ListShown"/> last filtered them.</summary>
    public IReadOnlyList<BrowseNode> Shown => _shown;

    public bool IsOpenAt(string panel, BrowseNode node)
    {
        if (!IsOpen)
            return false;
        if (Panel != panel)
            return false;
        return ReferenceEquals(Node, node);
    }

    /// <summary>Lists the children of <paramref name="node"/>, in the bar of <paramref name="panel"/>,
    /// unfiltered, with <paramref name="next"/>, the crumb after it, to be selected; its filter takes focus.</summary>
    public void Open(string panel, BrowseNode node, BrowseNode? next)
    {
        IsOpen = true;
        Panel = panel;
        Node = node;
        Filter = "";
        _children = null;
        _shown.Clear();
        Selection.Clear();
        _toSelect = next;
        _isFocusPending = true;
    }

    public void Close() => IsOpen = false;

    /// <summary>True once after <see cref="Open"/>: the filter should take focus.</summary>
    public bool TakeFocus()
    {
        var focus = _isFocusPending;
        _isFocusPending = false;
        return focus;
    }

    /// <summary>The <paramref name="children"/> whose name, as <paramref name="tree"/> labels their rows, holds
    /// the filter; filtered again only when the children or the filter changed.</summary>
    public IReadOnlyList<BrowseNode> ListShown(IReadOnlyList<BrowseNode> children, BrowseTree tree)
    {
        if (IsShowing(children))
            return _shown;
        _children = children;
        _shownFilter = Filter;
        _shown.Clear();
        _shown.AddRange(children.Where(c => Matches(tree.RowLabel(c))));
        SelectWaitingChild();
        Selection.DropIfGone(_shown);
        SelectFirstMatch();
        return _shown;
    }

    private bool IsShowing(IReadOnlyList<BrowseNode> children)
    {
        if (!ReferenceEquals(children, _children))
            return false;
        return Filter == _shownFilter;
    }

    private bool Matches(string name) => name.Contains(Filter, StringComparison.OrdinalIgnoreCase);

    /// <summary>Selects the crumb after this one once the children built hold it.</summary>
    private void SelectWaitingChild()
    {
        if (_toSelect == null)
            return;
        if (!_shown.Contains(_toSelect))
            return;
        Selection.Select(_toSelect, _shown);
        _toSelect = null;
    }

    /// <summary>Selects the first child listed while a filter leaves none selected, for Enter to go to.</summary>
    private void SelectFirstMatch()
    {
        if (Filter.Length == 0)
            return;
        if (Selection.Item != null)
            return;
        if (_shown.Count == 0)
            return;
        Selection.Select(_shown[0], _shown);
    }
}
