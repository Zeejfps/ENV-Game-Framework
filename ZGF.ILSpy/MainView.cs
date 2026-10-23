using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

/// <summary>The whole window: menu bar, toolbar, tree | documents and search, status bar, and the
/// dialogs over them.</summary>
public static class MainView
{
    public static void Render(Ui ui, Workspace w)
    {
        var theme = AppThemes.For(w.Theme);
        var t = w.GetStrings();
        using var screen = ui.Screen(theme.Ui);
        using var code = ui.Provide(theme.Code);
        using var icons = ui.Provide(theme.Icons);
        w.Sync();
        Shortcuts.Handle(ui, w);
        using (ui.Column(new Box { Height = Sizing.Grow(), Gap = 0f }))
        {
            MainMenuView.Render(ui, w, t);
            ui.Separator();
            ToolbarView.Render(ui, w, t);
            ui.Separator();
            Panels(ui, w, t);
            ui.Separator();
            StatusBarView.Render(ui, w, t);
        }
        FrameworkPickerView.Render(ui, w, t);
        BookmarkImportDialogView.Render(ui, w, t);
        LayoutResetDialogView.Render(ui, w, t);
        ErrorDialogView.Render(ui, w, t);
    }

    private static void Panels(Ui ui, Workspace w, Strings t)
    {
        UpdateSearch(ui, w);
        if (IsLayoutEmpty(w))
        {
            DocumentsView.Hint(ui, t.ViewEmptyHint);
            return;
        }
        DockSpace(ui, w, t);
        w.SyncActiveTab();
        RemoveReleasedImages(ui, w);
    }

    private static void UpdateSearch(Ui ui, Workspace w)
    {
        var shown = w.Layout.Contains(DockPanels.Search);
        var wait = w.Search.Update(w.Assemblies, ui.RealTime, shown);
        if (wait > 0)
            ui.RequestFrameIn(wait);
    }

    private static bool IsLayoutEmpty(Workspace w)
    {
        return !w.Layout.HasPanels();
    }

    private static void DockSpace(Ui ui, Workspace w, Strings t)
    {
        using var dock = ui.DockSpace("dock", w.Layout, (w, t), static (s, panel) => Tab(s.w, s.t, panel));
        while (dock.Next(out var panel))
            Panel(ui, w, t, panel);
    }

    private static DockTab Tab(Workspace w, Strings t, string panel)
    {
        if (DockPanels.IsView(panel))
            return DockPanels.Tab(t, panel);
        var tab = w.Tabs.Find(panel);
        if (tab != null)
            return DocumentsView.Tab(w, tab);
        return new DockTab(panel);
    }

    private static void Panel(Ui ui, Workspace w, Strings t, string panel)
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
            case DockPanels.Analyzer:
                AnalyzerPanelView.Render(ui, w, t);
                break;
            case DockPanels.DecompilerOptions:
                DecompilerOptionsPanelView.Render(ui, w, t);
                break;
            default:
                Document(ui, w, t, panel);
                break;
        }
    }

    private static void Document(Ui ui, Workspace w, Strings t, string panel)
    {
        var tab = w.Tabs.Find(panel);
        if (tab == null)
            return;
        DocumentsView.Render(ui, w, t, tab);
    }

    private static void RemoveReleasedImages(Ui ui, Workspace w)
    {
        foreach (var image in w.Tabs.TakeReleasedImages())
            ui.RemoveImage(image);
    }
}
