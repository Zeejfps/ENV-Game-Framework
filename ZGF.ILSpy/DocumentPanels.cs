using ZGF.Gui.Immediate.Widgets;

namespace ZGF.ILSpy;

/// <summary>The open tabs as panels of the layout: docking them, bringing them to the front, cycling and
/// closing them, and keeping a view for each open tab. What changes the tabs themselves is asked of the
/// app state through <see cref="UiState.AppCommands"/>.</summary>
public static class DocumentPanels
{
    /// <summary>Shows <paramref name="tab"/>, the app state's first tab, in the place of the documents the
    /// layout was restored with.</summary>
    public static void ShowInRestoredDocuments(UiState uiState, DocumentTab tab)
    {
        uiState.Documents.Add(tab, uiState.RestoredDocumentsPanel);
    }

    /// <summary>A new panel the layout makes for itself, with an empty tab asked for to be opened in it.</summary>
    internal static string OpenTabForPanel(UiState uiState)
    {
        var panel = uiState.Documents.NewPanel();
        uiState.AppCommands.Enqueue(new AppCommand.OpenTab(panel, Focus: false));
        return panel;
    }

    /// <summary>Called when a panel is closed: clears the search with its panel, and asks for the tab a
    /// closed document was to be closed.</summary>
    internal static void OnPanelClosed(UiState uiState, string panel)
    {
        if (panel == DockPanels.Search)
            uiState.Search.Clear();
        CloseTabOf(uiState, panel);
    }

    private static void CloseTabOf(UiState uiState, string panel)
    {
        var tab = uiState.Documents.FindTab(panel);
        if (tab == null)
            return;
        uiState.Documents.Remove(tab);
        uiState.AppCommands.Enqueue(new AppCommand.CloseTab(tab));
    }

    /// <summary>Focuses the tab of the panel the layout focuses now, else the first group's front tab.</summary>
    public static void FocusNextTab(UiState uiState)
    {
        var next = uiState.Documents.FindTab(uiState.Layout.Focused);
        if (next == null)
            next = FindFrontTab(uiState);
        if (next == null)
            return;
        AskToFocusTab(uiState, next);
    }

    /// <summary>The tab in front of the first group, docked then floating, that has one in front.
    /// Allocates nothing.</summary>
    private static DocumentTab? FindFrontTab(UiState uiState)
    {
        var layout = uiState.Layout;
        var docked = FindFrontTab(uiState, layout.Root);
        if (docked != null)
            return docked;
        foreach (var window in layout.Windows)
        {
            var floating = FindFrontTab(uiState, window.Root);
            if (floating != null)
                return floating;
        }
        return null;
    }

    private static DocumentTab? FindFrontTab(UiState uiState, DockNode node)
    {
        if (node is DockGroup group)
            return uiState.Documents.FindTab(group.Active);
        if (node is not DockSplit split)
            return null;
        var first = FindFrontTab(uiState, split.First);
        if (first != null)
            return first;
        return FindFrontTab(uiState, split.Second);
    }

    /// <summary>Asks for <paramref name="tab"/> to become the active tab.</summary>
    private static void AskToFocusTab(UiState uiState, DocumentTab tab)
    {
        uiState.AppCommands.Enqueue(new AppCommand.FocusTab(tab));
    }

    /// <summary>Focuses another tab when the active one was closed by a feature, trying again only once
    /// the tabs or the layout changed since the last try found none.</summary>
    public static void FocusNextTabIfNone(UiState uiState, DocumentTabs tabs)
    {
        if (tabs.Active != null)
            return;
        if (!uiState.NextTabFocus.TakeTry(uiState.Layout.Changes, tabs.Count))
            return;
        FocusNextTab(uiState);
    }

    /// <summary>Docks <paramref name="tab"/>, an open tab, beside the tab <paramref name="beside"/> when it
    /// is not docked yet, and brings it to the front.</summary>
    public static void ShowTab(UiState uiState, DocumentTab tab, DocumentTab? beside)
    {
        var documents = uiState.Documents;
        documents.Add(tab);
        var panel = documents.PanelOf(tab);
        if (!uiState.Layout.Contains(panel))
            uiState.MainLayout.DockDocument(panel, documents.FindPanel(beside));
        uiState.Layout.Activate(panel);
    }

    /// <summary>Brings <paramref name="tab"/>'s panel to the front and asks for it to become the active tab.</summary>
    public static void ActivateTab(UiState uiState, DocumentTab tab)
    {
        if (!uiState.Documents.Contains(tab))
            return;
        uiState.Layout.Activate(uiState.Documents.PanelOf(tab));
        AskToFocusTab(uiState, tab);
    }

    /// <summary>Called after the documents are drawn: asks for the tab the user last pressed, docked or
    /// brought to front to become the active one, unless it is <paramref name="active"/>.</summary>
    public static void SyncActiveTab(UiState uiState, DocumentTab? active)
    {
        FollowActiveTab(uiState, active);
        var tab = uiState.Documents.FindTab(uiState.Layout.Focused);
        if (tab == null)
            return;
        if (tab == active)
            return;
        AskToFocusTab(uiState, tab);
    }

    /// <summary>Takes <paramref name="active"/>, the app state's active tab, as the tab the views open beside.</summary>
    public static void FollowActiveTab(UiState uiState, DocumentTab? active)
    {
        uiState.ActiveTab = active;
    }

    /// <summary>Brings the next tab of the active tab's group to the front, or the previous one.</summary>
    public static void CycleTab(UiState uiState, int by)
    {
        var active = uiState.ActiveTab;
        if (active == null)
            return;
        var activePanel = uiState.Documents.PanelOf(active);
        var group = uiState.Layout.GroupOf(activePanel);
        if (group == null)
            return;
        var panels = group.Panels.FindAll(p => IsTabPanel(uiState, p));
        if (panels.Count < 2)
            return;
        var index = panels.IndexOf(activePanel);
        var next = panels[WrapIndex(index + by, panels.Count)];
        var tab = uiState.Documents.FindTab(next);
        if (tab == null)
            return;
        ActivateTab(uiState, tab);
    }

    private static bool IsTabPanel(UiState uiState, string panel) => uiState.Documents.FindTab(panel) != null;

    private static int WrapIndex(int index, int count)
    {
        var wrapped = index % count;
        if (wrapped < 0)
            wrapped += count;
        return wrapped;
    }

    public static void CloseActiveTab(UiState uiState)
    {
        var tab = uiState.ActiveTab;
        if (tab == null)
            return;
        uiState.Layout.Close(uiState.Documents.PanelOf(tab));
    }

    /// <summary>The panel of the active tab, which the views dock beside, or null.</summary>
    internal static string? FindActivePanel(UiState uiState) => uiState.Documents.FindPanel(uiState.ActiveTab);

    /// <summary>Drops the view of each tab closed, keeping its image to free, and closes its panel;
    /// makes a view for each tab opened; and lets each view follow the grid or picture its tab shows.</summary>
    public static void SyncTabViews(UiState uiState, DocumentTabs tabs)
    {
        DropClosedTabViews(uiState, tabs);
        AddOpenTabViews(uiState, tabs);
        uiState.Documents.FollowContent();
    }

    private static void DropClosedTabViews(UiState uiState, DocumentTabs tabs)
    {
        var documents = uiState.Documents;
        foreach (var tab in documents.ListClosed(tabs))
        {
            var panel = documents.PanelOf(tab);
            documents.Remove(tab);
            ClosePanel(uiState, panel);
        }
    }

    private static void ClosePanel(UiState uiState, string panel)
    {
        if (!uiState.Layout.Contains(panel))
            return;
        uiState.Layout.Close(panel);
    }

    private static void AddOpenTabViews(UiState uiState, DocumentTabs tabs)
    {
        for (var i = 0; i < tabs.Count; i++)
            uiState.Documents.Add(tabs[i]);
    }

    /// <summary>The tab in front of each group that has one.</summary>
    public static List<DocumentTab> ListFrontTabs(UiState uiState)
    {
        var tabs = new List<DocumentTab>();
        foreach (var group in uiState.Layout.AllGroups())
            AddFrontTab(uiState, tabs, group.Active);
        return tabs;
    }

    private static void AddFrontTab(UiState uiState, List<DocumentTab> tabs, string? panel)
    {
        var tab = uiState.Documents.FindTab(panel);
        if (tab == null)
            return;
        tabs.Add(tab);
    }
}
