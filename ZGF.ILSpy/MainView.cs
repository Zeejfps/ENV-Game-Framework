using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

/// <summary>The whole window: menu bar, toolbar, tree | documents and search, status bar, and the
/// dialogs over them.</summary>
public static class MainView
{
    public static void Render(Ui<UiState> ui, Workspace w)
    {
        using var screen = ui.Screen();
        Shortcuts.Handle(ui, w);
        using (ui.Column(new Box { Height = Sizing.Grow(), Gap = 0f }))
        {
            MainMenuView.Render(ui, w);
            ui.Separator();
            ToolbarView.Render(ui, w);
            ui.Separator();
            Panels(ui, w);
            ui.Separator();
            StatusBarView.Render(ui, w);
        }
        FrameworkPickerView.Render(ui, w);
        BookmarkImportDialogView.Render(ui, w);
        LayoutResetDialogView.Render(ui, w);
        ErrorDialogView.Render(ui, w);
    }

    private static void Panels(Ui<UiState> ui, Workspace w)
    {
        var t = ui.State.Strings;
        UpdateSearch(ui, w);
        if (IsLayoutEmpty(w))
        {
            DocumentsView.Hint(ui, t.ViewEmptyHint);
            return;
        }
        DockSpace(ui, w);
        w.SyncActiveTab();
        RemoveReleasedImages(ui, w);
    }

    private static void UpdateSearch(Ui<UiState> ui, Workspace w)
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

    private static void DockSpace(Ui<UiState> ui, Workspace w)
    {
        var t = ui.State.Strings;
        using var dock = ui.DockSpace("dock", w.Layout, (w, t), static (s, panel) => Tab(s.w, s.t, panel));
        while (dock.Next(out var panel))
            Panel(ui, w, panel);
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

    private static void Panel(Ui<UiState> ui, Workspace w, string panel)
    {
        switch (panel)
        {
            case DockPanels.Assemblies:
                TreePaneView.Render(ui, w);
                break;
            case DockPanels.Search:
                SearchPanelView.Render(ui, w);
                break;
            case DockPanels.Bookmarks:
                BookmarksPanelView.Render(ui, w);
                break;
            case DockPanels.Analyzer:
                AnalyzerPanelView.Render(ui, w);
                break;
            case DockPanels.DecompilerOptions:
                DecompilerOptionsPanelView.Render(ui, w);
                break;
            default:
                Document(ui, w, panel);
                break;
        }
    }

    private static void Document(Ui<UiState> ui, Workspace w, string panel)
    {
        var tab = w.Tabs.Find(panel);
        if (tab == null)
            return;
        DocumentsView.Render(ui, w, tab);
    }

    private static void RemoveReleasedImages(Ui<UiState> ui, Workspace w)
    {
        foreach (var image in w.Tabs.TakeReleasedImages())
            ui.RemoveImage(image);
    }
}
