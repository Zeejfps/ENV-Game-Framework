using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using static ZGF.ILSpy.DocumentPanels;

namespace ZGF.ILSpy;

/// <summary>The context menu of a document's tab: closing it, the other documents of its group, all of
/// them, or those to its left or right.</summary>
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

    public static void Render(Ui<UiState> ui)
    {
        using var menu = ui.ContextMenu(MenuId);
        if (!menu.IsOpen)
            return;
        var tab = ui.State.TabMenuTarget;
        if (tab == null)
            return;
        if (!ui.State.Documents.Contains(tab))
            return;
        TabMenu(ui, tab);
    }

    private static void TabMenu(Ui<UiState> ui, DocumentTab tab)
    {
        var t = ui.State.Strings;
        var screen = ui.State;
        var tabs = ListTabsInGroup(screen, tab);
        var index = tabs.IndexOf(tab);
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
}
