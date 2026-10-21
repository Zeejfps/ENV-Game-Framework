using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;

namespace ZGF.ILSpy;

/// <summary>The whole window: menu bar, toolbar, tree | documents and search, status bar, and the
/// dialogs over them.</summary>
public static class MainView
{
    public static void Render(Ui ui, Workspace w)
    {
        var theme = AppThemes.For(w.DarkMode);
        using var screen = ui.Screen(theme.Ui);
        using var code = ui.Provide(theme.Code);
        using var icons = ui.Provide(theme.Icons);
        w.Sync();
        Shortcuts.Handle(ui, w);
        using (ui.Column(new Box { Height = Sizing.Grow(), Gap = 0f }))
        {
            MainMenuView.Render(ui, w);
            ToolbarView.Render(ui, w);
            ui.Separator();
            using (var split = ui.SplitPane("main", Direction.Horizontal, ref w.TreeWidth, minFirst: AppLayout.TreeMinWidth,
                       minSecond: AppLayout.DocumentsMinWidth))
            {
                using (split.First())
                    TreePaneView.Render(ui, w);
                using (split.Second())
                    ContentPane(ui, w);
            }
            ui.Separator();
            StatusBarView.Render(ui, w);
        }
        FrameworkPickerView.Render(ui, w);
        ErrorDialogView.Render(ui, w);
    }

    private static void ContentPane(Ui ui, Workspace w)
    {
        var wait = w.Search.Update(w.Assemblies, ui.RealTime, w.Post);
        if (wait > 0)
            ui.RequestFrameIn(wait);

        if (!w.Search.IsOpen)
        {
            DocumentsView.Render(ui, w);
            return;
        }

        using var split = ui.SplitPane("search-split", Direction.Vertical, ref w.SearchHeight, minFirst: AppLayout.DocumentsMinHeight,
            minSecond: AppLayout.SearchPanelMinHeight, sized: SplitSide.Second);
        using (split.First())
            DocumentsView.Render(ui, w);
        using (split.Second())
            SearchPanelView.Render(ui, w);
    }
}
