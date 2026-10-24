namespace ZGF.ILSpy;

public sealed partial class UiState
{
    private readonly Dictionary<DocumentTab, TabView> _tabViews = new();
    private readonly List<DocumentTab> _closedTabs = new();
    private readonly List<string> _releasedImages = new();
    private int _documentPanels;

    /// <summary>How <paramref name="tab"/>, an open tab, is shown.</summary>
    public TabView ViewOf(DocumentTab tab) => _tabViews[tab];

    /// <summary>The panel <paramref name="tab"/>, an open tab, is docked as.</summary>
    public string PanelOf(DocumentTab tab) => ViewOf(tab).Panel;

    /// <summary>The panel <paramref name="tab"/> is docked as, or null when there is no tab or it has no view.</summary>
    private string? FindPanel(DocumentTab? tab)
    {
        if (tab == null)
            return null;
        if (!_tabViews.TryGetValue(tab, out var view))
            return null;
        return view.Panel;
    }

    /// <summary>The open tab docked as <paramref name="panel"/>. Allocates nothing.</summary>
    public DocumentTab? FindTab(string? panel)
    {
        foreach (var (tab, view) in _tabViews)
        {
            if (view.Panel == panel)
                return tab;
        }
        return null;
    }

    /// <summary>Drops the view of each tab closed, keeping its image to free, and closes its panel;
    /// makes a view for each tab opened; and lets each view follow the grid or picture its tab shows.</summary>
    public void SyncTabViews(DocumentTabs tabs)
    {
        DropClosedTabViews(tabs);
        AddOpenTabViews(tabs);
        FollowContent();
    }

    private void DropClosedTabViews(DocumentTabs tabs)
    {
        _closedTabs.Clear();
        foreach (var tab in _tabViews.Keys)
        {
            if (!tabs.Contains(tab))
                _closedTabs.Add(tab);
        }
        foreach (var tab in _closedTabs)
        {
            var panel = PanelOf(tab);
            DropTabView(tab);
            ClosePanel(panel);
        }
    }

    private void DropTabView(DocumentTab tab)
    {
        if (!_tabViews.Remove(tab, out var view))
            return;
        _releasedImages.Add(view.ImageId);
    }

    private void AddOpenTabViews(DocumentTabs tabs)
    {
        for (var i = 0; i < tabs.Count; i++)
            AddTabView(tabs[i]);
    }

    private void AddTabView(DocumentTab tab)
    {
        if (_tabViews.ContainsKey(tab))
            return;
        AddTabView(tab, NewDocumentPanel());
    }

    /// <summary>Shows <paramref name="tab"/> in <paramref name="panel"/>, unless it is shown already.</summary>
    public void AddTabView(DocumentTab tab, string panel)
    {
        if (_tabViews.ContainsKey(tab))
            return;
        _tabViews.Add(tab, new TabView(panel));
    }

    private string NewDocumentPanel()
    {
        _documentPanels++;
        return $"document-{_documentPanels}";
    }

    private void FollowContent()
    {
        foreach (var (tab, view) in _tabViews)
            view.Follow(tab);
    }

    /// <summary>Where the view of <paramref name="tab"/> stands, or null when it has none.</summary>
    public CodeViewPosition? FindViewPosition(DocumentTab tab)
    {
        if (!_tabViews.TryGetValue(tab, out var view))
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
}
