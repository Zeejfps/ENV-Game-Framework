using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using static ZGF.ILSpy.DocumentPanels;
using static ZGF.ILSpy.Navigation;

namespace ZGF.ILSpy;

/// <summary>The context menu of a document's tab: keeping the preview tab open, and closing the tab, the
/// other documents of its group, all of them, or those to its left or right.</summary>
internal static class TabMenuView
{
    private const string MenuId = "tab-menu";

    /// <summary>Opens the menu on the tab right-clicked, when it is a document's.</summary>
    public static void OpenOnRightClick(Ui<UiState> ui, DockTabMenu? request)
    {
        if (request == null)
            return;
        var tab = ui.State.Documents.FindTab(request.Value.Panel);
        if (tab == null)
            return;
        ui.State.TabMenuTarget = tab;
        ui.OpenContextMenu(MenuId, request.Value.At);
    }

    /// <summary>Keeps the tab double-clicked open, when it is a document's.</summary>
    public static void KeepOpenOnDoubleClick(Ui<UiState> ui, AppState appState, string? panel)
    {
        if (panel == null)
            return;
        var tab = ui.State.Documents.FindTab(panel);
        if (tab == null)
            return;
        KeepTabOpen(appState, tab);
    }

    public static void Render(Ui<UiState> ui, AppState appState)
    {
        using var menu = ui.ContextMenu(MenuId);
        if (!menu.IsOpen)
            return;
        var tab = ui.State.TabMenuTarget;
        if (tab == null)
            return;
        if (!ui.State.Documents.Contains(tab))
            return;
        TabMenu(ui, appState, tab);
    }

    private static void TabMenu(Ui<UiState> ui, AppState appState, DocumentTab tab)
    {
        var t = ui.State.Strings;
        var screen = ui.State;
        var tabs = ListTabsInGroup(screen, tab);
        var index = tabs.IndexOf(tab);
        KeepOpenItem(ui, appState, tab);
        if (ui.MenuItem(t.TabClose, Shortcuts.CloseTab, id: "close"))
            CloseTab(screen, tab);
        if (ui.MenuItem(t.TabCloseOthers, enabled: tabs.Count > 1, id: "close-others"))
            CloseOtherTabs(screen, tab);
        if (ui.MenuItem(t.TabCloseAll, id: "close-all"))
            CloseTabsInGroup(screen, tab);
        ui.MenuSeparator();
        if (ui.MenuItem(t.TabCloseLeft, enabled: index > 0, id: "close-left"))
            CloseTabsLeftOf(screen, tab);
        if (ui.MenuItem(t.TabCloseRight, enabled: index < tabs.Count - 1, id: "close-right"))
            CloseTabsRightOf(screen, tab);
    }

    /// <summary>The item keeping the tab open, with a separator after it, while it is the preview tab.</summary>
    private static void KeepOpenItem(Ui<UiState> ui, AppState appState, DocumentTab tab)
    {
        if (tab != appState.Tabs.Preview)
            return;
        if (ui.MenuItem(ui.State.Strings.TabKeepOpen, id: "keep-open"))
            KeepTabOpen(appState, tab);
        ui.MenuSeparator();
    }
}
