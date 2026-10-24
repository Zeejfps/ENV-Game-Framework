namespace ZGF.ILSpy;

public sealed partial class UiState
{
    private readonly Dictionary<DocumentTab, TabView> _tabViews = new();
    private readonly List<DocumentTab> _closedTabs = new();
    private readonly List<string> _releasedImages = new();

    /// <summary>How <paramref name="tab"/>, an open tab, is shown.</summary>
    public TabView ViewOf(DocumentTab tab) => _tabViews[tab];

    /// <summary>Drops the view of each tab closed, keeping its image to free, makes a view for each
    /// tab opened, and lets each view follow the grid or picture its tab shows.</summary>
    private void SyncTabViews()
    {
        DropClosedTabViews();
        AddOpenTabViews();
        FollowContent();
    }

    private void DropClosedTabViews()
    {
        _closedTabs.Clear();
        foreach (var tab in _tabViews.Keys)
        {
            if (!_workspace.Tabs.Contains(tab))
                _closedTabs.Add(tab);
        }
        foreach (var tab in _closedTabs)
            DropTabView(tab);
    }

    private void DropTabView(DocumentTab tab)
    {
        var view = _tabViews[tab];
        _tabViews.Remove(tab);
        _releasedImages.Add(view.ImageId);
    }

    private void AddOpenTabViews()
    {
        var tabs = _workspace.Tabs;
        for (var i = 0; i < tabs.Count; i++)
            AddTabView(tabs[i]);
    }

    private void AddTabView(DocumentTab tab)
    {
        if (_tabViews.ContainsKey(tab))
            return;
        _tabViews.Add(tab, new TabView(tab));
    }

    private void FollowContent()
    {
        foreach (var (tab, view) in _tabViews)
            view.Follow(tab);
    }

    private void KeepTabPositions()
    {
        foreach (var (tab, view) in _tabViews)
            tab.KeepPosition(view.PositionOf(tab));
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
