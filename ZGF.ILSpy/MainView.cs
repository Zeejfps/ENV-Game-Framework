using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

/// <summary>The whole window: menu bar, toolbar, tree | documents and search, status bar, and the
/// dialogs over them.</summary>
public static class MainView
{
    public static void Render(Ui<UiState> ui, AppState w)
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
        AssemblyListsDialogView.Render(ui, w);
        AssemblyListDeleteDialogView.Render(ui, w);
        BookmarkImportDialogView.Render(ui, w);
        LayoutResetDialogView.Render(ui, w);
        ExportDialogView.Render(ui, w);
        ErrorDialogView.Render(ui, w);
    }

    private static void Panels(Ui<UiState> ui, AppState w)
    {
        var t = ui.State.Strings;
        UpdateSearch(ui, w);
        if (IsLayoutEmpty(ui.State))
        {
            DocumentsView.Hint(ui, t.ViewEmptyHint);
            return;
        }
        DockSpace(ui, w);
        ui.State.SyncActiveTab();
        RemoveReleasedImages(ui);
    }

    private static void UpdateSearch(Ui<UiState> ui, AppState w)
    {
        var shown = ui.State.Layout.Contains(DockPanels.Search);
        var wait = ui.State.Search.Update(w.Assemblies, ui.RealTime, shown);
        if (wait > 0)
            ui.RequestUpdateIn(wait);
    }

    private static bool IsLayoutEmpty(UiState screen)
    {
        return !screen.Layout.HasPanels();
    }

    private static void DockSpace(Ui<UiState> ui, AppState w)
    {
        var t = ui.State.Strings;
        using var dock = ui.DockSpace("dock", ui.State.Layout, (ui.State, w, t), static (s, panel) => Tab(s.State, s.w, s.t, panel));
        while (dock.Next(out var panel))
            Panel(ui, w, panel);
    }

    private static DockTab Tab(UiState screen, AppState w, Strings t, string panel)
    {
        if (DockPanels.IsView(panel))
            return DockPanels.Tab(t, panel);
        var tab = w.Tabs.Find(panel);
        if (tab != null)
            return DocumentsView.Tab(screen, w, tab);
        return new DockTab(panel);
    }

    private static void Panel(Ui<UiState> ui, AppState w, string panel)
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
            case DockPanels.Exports:
                ExportsPanelView.Render(ui, w);
                break;
            default:
                Document(ui, w, panel);
                break;
        }
    }

    private static void Document(Ui<UiState> ui, AppState w, string panel)
    {
        var tab = w.Tabs.Find(panel);
        if (tab == null)
            return;
        DocumentsView.Render(ui, w, tab);
    }

    private static void RemoveReleasedImages(Ui<UiState> ui)
    {
        foreach (var image in ui.State.TakeReleasedImages())
            ui.RemoveImage(image);
    }
}
