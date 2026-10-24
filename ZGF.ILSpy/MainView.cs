using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

/// <summary>The whole window: menu bar, toolbar, tree | documents and search, status bar, and the
/// dialogs over them.</summary>
public static class MainView
{
    public static void Render(Ui<UiState> ui, AppState appState)
    {
        using var screen = ui.Screen();
        Shortcuts.Handle(ui, appState);
        using (ui.Column(new Box { Height = Sizing.Grow(), Gap = 0f }))
        {
            MainMenuView.Render(ui, appState);
            ui.Separator();
            ToolbarView.Render(ui, appState);
            ui.Separator();
            Panels(ui, appState);
            ui.Separator();
            StatusBarView.Render(ui, appState);
        }
        FrameworkPickerView.Render(ui, appState);
        AssemblyListsDialogView.Render(ui, appState);
        AssemblyListDeleteDialogView.Render(ui, appState);
        BookmarkImportDialogView.Render(ui, appState);
        LayoutResetDialogView.Render(ui, appState);
        ExportDialogView.Render(ui, appState);
        ErrorDialogView.Render(ui, appState);
    }

    private static void Panels(Ui<UiState> ui, AppState appState)
    {
        var t = ui.State.Strings;
        UpdateSearch(ui, appState);
        if (IsLayoutEmpty(ui.State))
        {
            DocumentsView.Hint(ui, t.ViewEmptyHint);
            return;
        }
        DockSpace(ui, appState);
        ui.State.SyncActiveTab();
        RemoveReleasedImages(ui);
    }

    private static void UpdateSearch(Ui<UiState> ui, AppState appState)
    {
        var shown = ui.State.Layout.Contains(DockPanels.Search);
        var wait = ui.State.Search.Update(appState.Assemblies, ui.RealTime, shown);
        if (wait > 0)
            ui.RequestUpdateIn(wait);
    }

    private static bool IsLayoutEmpty(UiState screen)
    {
        return !screen.Layout.HasPanels();
    }

    private static void DockSpace(Ui<UiState> ui, AppState appState)
    {
        var t = ui.State.Strings;
        using var dock = ui.DockSpace("dock", ui.State.Layout, (ui.State, appState, t), static (s, panel) => Tab(s.State, s.appState, s.t, panel));
        while (dock.Next(out var panel))
            Panel(ui, appState, panel);
    }

    private static DockTab Tab(UiState screen, AppState appState, Strings t, string panel)
    {
        if (DockPanels.IsView(panel))
            return DockPanels.Tab(t, panel);
        var tab = appState.Tabs.Find(panel);
        if (tab != null)
            return DocumentsView.Tab(screen, appState, tab);
        return new DockTab(panel);
    }

    private static void Panel(Ui<UiState> ui, AppState appState, string panel)
    {
        switch (panel)
        {
            case DockPanels.Assemblies:
                TreePaneView.Render(ui, appState);
                break;
            case DockPanels.Search:
                SearchPanelView.Render(ui, appState);
                break;
            case DockPanels.Bookmarks:
                BookmarksPanelView.Render(ui, appState);
                break;
            case DockPanels.Analyzer:
                AnalyzerPanelView.Render(ui, appState);
                break;
            case DockPanels.DecompilerOptions:
                DecompilerOptionsPanelView.Render(ui, appState);
                break;
            case DockPanels.Exports:
                ExportsPanelView.Render(ui, appState);
                break;
            default:
                Document(ui, appState, panel);
                break;
        }
    }

    private static void Document(Ui<UiState> ui, AppState appState, string panel)
    {
        var tab = appState.Tabs.Find(panel);
        if (tab == null)
            return;
        DocumentsView.Render(ui, appState, tab);
    }

    private static void RemoveReleasedImages(Ui<UiState> ui)
    {
        foreach (var image in ui.State.TakeReleasedImages())
            ui.RemoveImage(image);
    }
}
