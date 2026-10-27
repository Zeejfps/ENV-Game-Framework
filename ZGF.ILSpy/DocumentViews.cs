using ZGF.ILSpy.Core;

namespace ZGF.ILSpy;

/// <summary>How each open tab is shown, found by the tab or by the panel it is docked as: each tab with a
/// view has a panel of its own. The images of the views dropped wait here until the canvas frees them.</summary>
public sealed class DocumentViews
{
    private readonly Dictionary<DocumentTab, TabView> _views = new();
    private readonly List<DocumentTab> _closed = new();
    private readonly List<string> _releasedImages = new();
    private int _panels;

    /// <summary>How <paramref name="tab"/>, an open tab, is shown.</summary>
    public TabView ViewOf(DocumentTab tab) => _views[tab];

    /// <summary>The panel <paramref name="tab"/>, an open tab, is docked as.</summary>
    public string PanelOf(DocumentTab tab) => ViewOf(tab).Panel;

    public bool Contains(DocumentTab tab) => _views.ContainsKey(tab);

    /// <summary>The panel <paramref name="tab"/> is docked as, or null when there is no tab or it has no view.</summary>
    public string? FindPanel(DocumentTab? tab)
    {
        if (tab == null)
            return null;
        if (!_views.TryGetValue(tab, out var view))
            return null;
        return view.Panel;
    }

    /// <summary>The open tab docked as <paramref name="panel"/>. Allocates nothing.</summary>
    public DocumentTab? FindTab(string? panel)
    {
        foreach (var (tab, view) in _views)
        {
            if (view.Panel == panel)
                return tab;
        }
        return null;
    }

    /// <summary>A document panel no tab was shown in before.</summary>
    public string NewPanel()
    {
        _panels++;
        return $"document-{_panels}";
    }

    /// <summary>Shows <paramref name="tab"/> in a new panel, unless it is shown already.</summary>
    public void Add(DocumentTab tab)
    {
        if (Contains(tab))
            return;
        Add(tab, NewPanel());
    }

    /// <summary>Shows <paramref name="tab"/> in <paramref name="panel"/>, unless it is shown already.</summary>
    public void Add(DocumentTab tab, string panel)
    {
        if (Contains(tab))
            return;
        _views.Add(tab, new TabView(panel));
    }

    /// <summary>Drops the view of <paramref name="tab"/>, keeping its image to free.</summary>
    public void Remove(DocumentTab tab)
    {
        if (!_views.Remove(tab, out var view))
            return;
        _releasedImages.Add(view.ImageId);
    }

    /// <summary>The tabs with a view that <paramref name="tabs"/> no longer holds, in a list the next call reuses.</summary>
    public IReadOnlyList<DocumentTab> ListClosed(DocumentTabs tabs)
    {
        _closed.Clear();
        foreach (var tab in _views.Keys)
        {
            if (!tabs.Contains(tab))
                _closed.Add(tab);
        }
        return _closed;
    }

    /// <summary>Lets each view follow the grid or picture its tab shows.</summary>
    public void FollowContent()
    {
        foreach (var (tab, view) in _views)
            view.Follow(tab);
    }

    /// <summary>Where the view of <paramref name="tab"/> stands, or null when it has none.</summary>
    public CodeViewPosition? FindPosition(DocumentTab tab)
    {
        if (!_views.TryGetValue(tab, out var view))
            return null;
        return view.PositionOf(tab);
    }

    /// <summary>The images of the views dropped since the last call, for the canvas to free.</summary>
    public IReadOnlyList<string> TakeReleasedImages()
    {
        if (_releasedImages.Count == 0)
            return Array.Empty<string>();
        var released = _releasedImages.ToArray();
        _releasedImages.Clear();
        return released;
    }

    /// <summary>Returns the view of <paramref name="tab"/>, an open tab, to <paramref name="position"/>.</summary>
    public void RestoreView(DocumentTab tab, CodeViewPosition position)
    {
        var view = _views.GetValueOrDefault(tab);
        if (view == null)
            return;
        if (tab.Grid == null)
        {
            view.Code.Restore(position);
            return;
        }
        view.Grid.Restore(position);
    }

    /// <summary>Scrolls the code of <paramref name="tab"/>, an open tab, to <paramref name="line"/> and flashes it.</summary>
    public void RevealLine(DocumentTab tab, int line)
    {
        var view = _views.GetValueOrDefault(tab);
        if (view == null)
            return;
        if (line >= tab.Document.Lines.Length)
            return;
        view.Code.RevealLine(line);
    }

    /// <summary>Selects the grid row <paramref name="row"/> of <paramref name="tab"/>, an open tab, and scrolls to it.</summary>
    public void RevealRow(DocumentTab tab, int row)
    {
        var view = _views.GetValueOrDefault(tab);
        if (view == null)
            return;
        if (!HasRow(tab, row))
            return;
        view.Grid.Reveal(row);
    }

    /// <summary>Selects the card of <paramref name="type"/> on the diagram <paramref name="tab"/>, an open tab,
    /// shows, and brings it into view once it is laid out.</summary>
    public void FocusCard(DocumentTab tab, CodeReference type)
    {
        var view = _views.GetValueOrDefault(tab);
        if (view == null)
            return;
        view.Diagram.Focus(type);
    }

    private static bool HasRow(DocumentTab tab, int row)
    {
        var grid = tab.Grid;
        if (grid == null)
            return false;
        return row < grid.RowCount;
    }
}
