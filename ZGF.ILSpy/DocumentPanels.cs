using ZGF.Gui.Immediate.Widgets;

namespace ZGF.ILSpy;

/// <summary>The open tabs as panels of the layout: docking them, the reopened ones where they were docked,
/// bringing them to the front, cycling and closing them, and keeping a view for each open tab. What changes the tabs themselves is asked of the
/// app state through <see cref="UiState.AppCommands"/>.</summary>
public static class DocumentPanels
{
    /// <summary>Shows the reopened <paramref name="tabs"/> in the panels they were docked as: in place where
    /// the layout still has one no tab is shown in, else, renamed when another tab is shown in one by that
    /// name, arranged as <paramref name="layout"/>, the documents' saved arrangement, has them, beside the
    /// documents; returns their diagrams' views to how they were saved, and brings the active tab to the front.</summary>
    public static void ShowReopenedTabs(UiState uiState, IReadOnlyList<ReopenedTab> tabs, DockLayout? layout)
    {
        var panels = new Dictionary<string, string>();
        foreach (var reopened in tabs)
            panels[reopened.Record.Panel] = AddReopenedView(uiState, reopened);
        DockUndocked(uiState, panels, layout);
        ActivateActivePanel(uiState);
    }

    /// <summary>Makes the view of the reopened tab, as it was saved, and returns the panel it is shown in.</summary>
    private static string AddReopenedView(UiState uiState, ReopenedTab reopened)
    {
        var panel = ChooseReopenedPanel(uiState, reopened.Record.Panel);
        uiState.Documents.Add(reopened.Tab, panel);
        RestoreDiagramView(uiState, reopened);
        RestoreComparisonView(uiState, reopened);
        return panel;
    }

    /// <summary>The panel <paramref name="saved"/>, unless a tab is shown in it; else a new one.</summary>
    private static string ChooseReopenedPanel(UiState uiState, string saved)
    {
        if (!IsTabPanel(uiState, saved))
            return saved;
        return uiState.Documents.NewPanel();
    }

    private static void RestoreDiagramView(UiState uiState, ReopenedTab reopened)
    {
        if (reopened.Record is not DiagramDocumentRecord diagram)
            return;
        var view = diagram.View;
        if (view == null)
            return;
        uiState.Documents.RestoreDiagramView(reopened.Tab, view);
    }

    private static void RestoreComparisonView(UiState uiState, ReopenedTab reopened)
    {
        if (reopened.Record is not ComparisonDocumentRecord comparison)
            return;
        var view = comparison.View;
        if (view == null)
            return;
        uiState.Documents.RestoreComparisonView(reopened.Tab, view);
    }

    /// <summary>Docks the panels of <paramref name="panels"/>, by the names they were saved as, that the
    /// layout does not have, as <paramref name="layout"/> arranges them.</summary>
    private static void DockUndocked(UiState uiState, Dictionary<string, string> panels, DockLayout? layout)
    {
        var undocked = panels.Values
            .Where(p => !uiState.Layout.Contains(p))
            .ToList();
        if (undocked.Count == 0)
            return;
        var arrangement = ArrangeUndocked(undocked, panels, layout);
        uiState.MainLayout.DockArranged(undocked, arrangement);
    }

    /// <summary>A copy of <paramref name="layout"/> renamed as <paramref name="panels"/> says and holding
    /// <paramref name="undocked"/> alone; one group of them when there is no layout.</summary>
    private static DockLayout ArrangeUndocked(List<string> undocked, Dictionary<string, string> panels, DockLayout? layout)
    {
        if (layout == null)
            return ArrangeInOneGroup(undocked);
        var arrangement = layout.CopyArrangement();
        DockPanels.Rename(arrangement, panels);
        arrangement.Retain(undocked.Contains);
        return arrangement;
    }

    private static DockLayout ArrangeInOneGroup(List<string> panels)
    {
        var group = DockLayout.Group(panels.ToArray());
        return new DockLayout { Root = group };
    }

    /// <summary>Brings the active tab's panel to the front and focuses it, when the layout has it.</summary>
    internal static void ActivateActivePanel(UiState uiState)
    {
        var panel = FindActivePanel(uiState);
        if (panel == null)
            return;
        if (!uiState.Layout.Contains(panel))
            return;
        uiState.Layout.Activate(panel);
    }

    /// <summary>Shows each open tab no panel was given, such as the one a run starts with when the open
    /// list kept no documents, in the place of the documents the layout was restored with, then takes the
    /// panels of the documents no tab is shown in out of the layout.</summary>
    public static void ShowInRestoredDocuments(UiState uiState, DocumentTabs tabs)
    {
        for (var i = 0; i < tabs.Count; i++)
            ShowInRestoredPlace(uiState, tabs[i]);
        uiState.Layout.Retain(p => IsShownPanel(uiState, p));
    }

    private static void ShowInRestoredPlace(UiState uiState, DocumentTab tab)
    {
        if (uiState.Documents.Contains(tab))
            return;
        var panel = uiState.Documents.NewPanel();
        uiState.Documents.Add(tab, panel);
        if (DockPanels.PlaceDocument(uiState.Layout, panel, p => IsTabPanel(uiState, p)))
            return;
        uiState.MainLayout.DockDocument(panel, panel);
    }

    /// <summary>Whether <paramref name="panel"/> is a view, or a tab is shown in it.</summary>
    private static bool IsShownPanel(UiState uiState, string panel)
    {
        if (DockPanels.IsView(panel))
            return true;
        return IsTabPanel(uiState, panel);
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
        CloseTab(uiState, tab);
    }

    /// <summary>Closes <paramref name="tab"/>'s panel, which asks for the tab to be closed.</summary>
    public static void CloseTab(UiState uiState, DocumentTab tab)
    {
        uiState.Layout.Close(uiState.Documents.PanelOf(tab));
    }

    /// <summary>Closes the document tabs of <paramref name="tab"/>'s group but <paramref name="tab"/>.</summary>
    public static void CloseOtherTabs(UiState uiState, DocumentTab tab)
    {
        var others = ListTabsInGroup(uiState, tab);
        others.Remove(tab);
        CloseTabs(uiState, others);
    }

    /// <summary>Closes every document tab of <paramref name="tab"/>'s group.</summary>
    public static void CloseTabsInGroup(UiState uiState, DocumentTab tab)
    {
        var all = ListTabsInGroup(uiState, tab);
        CloseTabs(uiState, all);
    }

    /// <summary>Closes the document tabs before <paramref name="tab"/> in its group.</summary>
    public static void CloseTabsLeftOf(UiState uiState, DocumentTab tab)
    {
        var tabs = ListTabsInGroup(uiState, tab);
        var index = tabs.IndexOf(tab);
        var left = tabs.GetRange(0, index);
        CloseTabs(uiState, left);
    }

    /// <summary>Closes the document tabs after <paramref name="tab"/> in its group.</summary>
    public static void CloseTabsRightOf(UiState uiState, DocumentTab tab)
    {
        var tabs = ListTabsInGroup(uiState, tab);
        var after = tabs.IndexOf(tab) + 1;
        var right = tabs.GetRange(after, tabs.Count - after);
        CloseTabs(uiState, right);
    }

    /// <summary>Closes the document tabs of every group, docked or floating.</summary>
    public static void CloseAllTabs(UiState uiState)
    {
        var all = new List<DocumentTab>();
        foreach (var group in uiState.Layout.AllGroups())
            AddTabsOf(uiState, all, group);
        CloseTabs(uiState, all);
    }

    /// <summary>The document tabs of <paramref name="tab"/>'s group, in the order its strip shows
    /// them, leaving out the other panels docked there.</summary>
    public static List<DocumentTab> ListTabsInGroup(UiState uiState, DocumentTab tab)
    {
        var tabs = new List<DocumentTab>();
        var group = uiState.Layout.GroupOf(uiState.Documents.PanelOf(tab));
        if (group == null)
            return tabs;
        AddTabsOf(uiState, tabs, group);
        return tabs;
    }

    /// <summary>Adds the document tabs of <paramref name="group"/> to <paramref name="tabs"/>, in the
    /// order its strip shows them.</summary>
    private static void AddTabsOf(UiState uiState, List<DocumentTab> tabs, DockGroup group)
    {
        foreach (var panel in group.Panels)
            AddTabOf(uiState, tabs, panel);
    }

    /// <summary>Adds the tab of <paramref name="panel"/> to <paramref name="tabs"/> when it is a document's.</summary>
    private static void AddTabOf(UiState uiState, List<DocumentTab> tabs, string? panel)
    {
        var tab = uiState.Documents.FindTab(panel);
        if (tab == null)
            return;
        tabs.Add(tab);
    }

    private static void CloseTabs(UiState uiState, List<DocumentTab> tabs)
    {
        foreach (var tab in tabs)
            CloseTab(uiState, tab);
    }

    /// <summary>The panel of the active tab, which the views dock beside, or null.</summary>
    internal static string? FindActivePanel(UiState uiState) => uiState.Documents.FindPanel(uiState.ActiveTab);

    /// <summary>Lets each view follow the grid or picture its tab shows.</summary>
    public static void FollowTabContent(UiState uiState)
    {
        uiState.Documents.FollowContent();
    }

    /// <summary>Drops the view of each tab closed, keeping its image to free, and closes its panel; and
    /// makes a view for each tab opened that has none yet.</summary>
    public static void SyncTabViews(UiState uiState, DocumentTabs tabs)
    {
        DropClosedTabViews(uiState, tabs);
        AddOpenTabViews(uiState, tabs);
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
            AddTabOf(uiState, tabs, group.Active);
        return tabs;
    }
}
