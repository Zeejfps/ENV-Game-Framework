using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

/// <summary>The whole window: menu bar, toolbar, tree | documents and search, status bar, and the
/// dialogs over them.</summary>
public static class MainView
{
    public static void Render(Ui ui, Workspace w, Strings t)
    {
        var theme = AppThemes.For(w.DarkMode);
        using var screen = ui.Screen(theme.Ui);
        using var code = ui.Provide(theme.Code);
        using var icons = ui.Provide(theme.Icons);
        w.Sync();
        Shortcuts.Handle(ui, w);
        using (ui.Column(new Box { Height = Sizing.Grow(), Gap = 0f }))
        {
            MainMenuView.Render(ui, w, t);
            ToolbarView.Render(ui, w, t);
            ui.Separator();
            Panels(ui, w, t);
            ui.Separator();
            StatusBarView.Render(ui, w, t);
        }
        FrameworkPickerView.Render(ui, w, t);
        BookmarkImportDialogView.Render(ui, w, t);
        ErrorDialogView.Render(ui, w, t);
    }

    private static void Panels(Ui ui, Workspace w, Strings t)
    {
        var wait = w.Search.Update(w.Assemblies, ui.RealTime, w.Post);
        if (wait > 0)
            ui.RequestFrameIn(wait);
        w.SyncSearchPanel();
        if (!w.Layout.AllGroups().Any(static g => g.Panels.Count > 0))
        {
            DocumentsView.Hint(ui, t.ViewEmptyHint);
            return;
        }
        using (var dock = ui.DockSpace("dock", w.Layout, (w, t), static (s, panel) => Tab(s.w, s.t, panel)))
        {
            while (dock.Next(out var panel))
            {
                switch (panel)
                {
                    case DockPanels.Assemblies:
                        TreePaneView.Render(ui, w, t);
                        break;
                    case DockPanels.Search:
                        SearchPanelView.Render(ui, w, t);
                        break;
                    case DockPanels.Bookmarks:
                        BookmarksPanelView.Render(ui, w, t);
                        break;
                    default:
                        if (w.FindTab(panel) is { } tab)
                            DocumentsView.Render(ui, w, t, tab);
                        break;
                }
            }
        }
        w.SyncActiveTab();
    }

    private static DockTab Tab(Workspace w, Strings t, string panel) =>
        DockPanels.IsView(panel) ? DockPanels.Tab(t, panel)
        : w.FindTab(panel) is { } tab ? DocumentsView.Tab(w, tab)
        : new DockTab(panel);
}
