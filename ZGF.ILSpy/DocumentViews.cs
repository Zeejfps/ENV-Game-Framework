using ZGF.ILSpy.Core;

namespace ZGF.ILSpy;

/// <summary>How each open tab is shown, found by the tab or by the panel it is docked as: each tab with a
/// view has a panel of its own, and a new panel is never one a saved document was docked as. The images of
/// the views dropped wait here until the canvas frees them.</summary>
public sealed class DocumentViews
{
    private const string PanelPrefix = "document-";

    private readonly Dictionary<DocumentTab, TabView> _views = new();
    private readonly List<DocumentTab> _closed = new();
    private readonly List<string> _releasedImages = new();
    private readonly HashSet<string> _skipped = new();
    private int _panels;
    private int _changes;
    private int _droppedChanges;

    /// <summary>Counts up whenever a view is made or dropped, or a view's diagram or comparison is viewed
    /// otherwise as <see cref="DiagramViewState.Changes"/> and <see cref="ComparisonRows.Changes"/> count; it
    /// never counts down. Allocates nothing.</summary>
    public int Revision
    {
        get
        {
            var revision = _changes + _droppedChanges;
            foreach (var view in _views.Values)
                revision += CountChanges(view);
            return revision;
        }
    }

    private static int CountChanges(TabView view) => view.Diagram.Changes + view.Comparison.Changes;

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

    /// <summary>A document panel no tab was shown in before, nor any of those <see cref="SkipPanels"/> was given.</summary>
    public string NewPanel()
    {
        var panel = NextPanel();
        while (_skipped.Contains(panel))
            panel = NextPanel();
        return panel;
    }

    private string NextPanel()
    {
        _panels++;
        return $"{PanelPrefix}{_panels}";
    }

    /// <summary>Leaves <paramref name="panels"/>, which saved documents were docked as, to them:
    /// <see cref="NewPanel"/> hands out none of them.</summary>
    public void SkipPanels(IEnumerable<string> panels)
    {
        foreach (var panel in panels)
            _skipped.Add(panel);
    }

    /// <summary>The panel each tab with a view is docked as, and how its diagram or comparison is viewed,
    /// for the settings to keep.</summary>
    public List<TabPanel> SavePanels()
    {
        var panels = new List<TabPanel>();
        foreach (var (tab, view) in _views)
            panels.Add(new TabPanel(tab, view.Panel, SaveDiagramView(tab, view), SaveComparisonView(tab, view)));
        return panels;
    }

    private static DiagramViewRecord? SaveDiagramView(DocumentTab tab, TabView view)
    {
        if (tab.Diagram == null)
            return null;
        return view.Diagram.Save();
    }

    private static ComparisonViewRecord? SaveComparisonView(DocumentTab tab, TabView view)
    {
        if (tab.Comparison == null)
            return null;
        return view.Comparison.Save();
    }

    /// <summary>Shows <paramref name="tab"/> in a new panel, unless it is shown already.</summary>
    public void Add(DocumentTab tab)
    {
        if (Contains(tab))
            return;
        Add(tab, NewPanel());
    }

    /// <summary>Shows <paramref name="tab"/> in <paramref name="panel"/>, unless it is shown already, its view
    /// following what the tab shows from the start, so what is asked of the view before it is next drawn,
    /// such as a row to reveal, is not undone when it first takes the tab's content.</summary>
    public void Add(DocumentTab tab, string panel)
    {
        if (Contains(tab))
            return;
        var view = new TabView(panel);
        view.Follow(tab);
        _views.Add(tab, view);
        _changes++;
    }

    /// <summary>Drops the view of <paramref name="tab"/>, keeping its image to free.</summary>
    public void Remove(DocumentTab tab)
    {
        if (!_views.Remove(tab, out var view))
            return;
        _releasedImages.Add(view.ImageId);
        _droppedChanges += CountChanges(view);
        _changes++;
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

    /// <summary>Returns the view of the diagram <paramref name="tab"/>, an open tab, shows to how
    /// <paramref name="saved"/> says it was.</summary>
    public void RestoreDiagramView(DocumentTab tab, DiagramViewRecord saved)
    {
        var view = _views.GetValueOrDefault(tab);
        if (view == null)
            return;
        view.Diagram.Restore(saved);
    }

    /// <summary>Returns the view of the comparison <paramref name="tab"/>, an open tab, shows to how
    /// <paramref name="saved"/> says it was.</summary>
    public void RestoreComparisonView(DocumentTab tab, ComparisonViewRecord saved)
    {
        var view = _views.GetValueOrDefault(tab);
        if (view == null)
            return;
        view.Comparison.Restore(saved);
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
