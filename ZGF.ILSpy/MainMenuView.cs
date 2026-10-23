using ZGF.Desktop;
using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

internal static class MainMenuView
{
    public static void Render(Ui<UiState> ui, Workspace w)
    {
        var t = ui.State.Strings;
        using (ui.MenuBar("menu", new Box { Background = ui.Theme.Background }))
        {
            using (var file = ui.Menu(t.MenuFile, id: "file"))
            {
                if (file.IsOpen)
                    FileMenu(ui, w);
            }
            using (var view = ui.Menu(t.MenuView, id: "view"))
            {
                if (view.IsOpen)
                    ViewMenu(ui, w);
            }
        }
    }

    public static void OpenFile(Ui<UiState> ui, Workspace w)
    {
        var t = ui.State.Strings;
        FileFilter[] filters =
        [
            new(t.FileFilterAssemblies, ["*.dll", "*.exe", "*.winmd"]),
            new(t.FileFilterAll, ["*.*"]),
        ];
        ui.PickFile(t.FileOpenTitle, null, filters, w.Open);
    }

    private static void FileMenu(Ui<UiState> ui, Workspace w)
    {
        var t = ui.State.Strings;
        if (ui.MenuItem(t.FileOpen, Shortcuts.Open, id: "open"))
            OpenFile(ui, w);
        if (ui.MenuItem(t.FileOpenFromFramework, id: "open-from-framework"))
            w.Picker.Open();
        ui.MenuSeparator();
        ExportItems(ui, w);
        ui.MenuSeparator();
        var selected = w.Tree.View.HasSelection ? w.Tree.View.Selected!.Assembly : null;
        if (ui.MenuItem(t.FileClose, enabled: selected != null, id: "close"))
            w.Close(selected!);
        if (ui.MenuItem(t.FileClearList, enabled: w.Tree.Roots.Count > 0, id: "clear-list"))
            w.ClearList();
        if (ui.MenuItem(t.FileReloadAll, Shortcuts.ReloadAll, enabled: w.Tree.Roots.Count > 0, id: "reload-all"))
            w.Refresh();
        ui.MenuSeparator();
        if (ui.MenuItem(t.FileExit, "Alt+F4", id: "exit"))
            ui.Quit();
    }

    private static void ExportItems(Ui<UiState> ui, Workspace w)
    {
        var t = ui.State.Strings;
        var canSave = w.CanSaveCode(w.Tabs.Active?.Node);
        if (ui.MenuItem(t.FileSaveCode, Shortcuts.SaveCode, enabled: canSave, id: "save-code"))
            SaveDialogs.PickActiveCodeFile(ui, w);
        var canExportAll = w.Tree.Roots.Count > 0 && !w.IsExporting;
        if (ui.MenuItem(t.FileExportSolution, enabled: canExportAll, id: "export-solution"))
            SaveDialogs.PickSolutionFile(ui, w);
        if (ui.MenuItem(t.FileGeneratePdbs, enabled: canExportAll, id: "generate-pdbs"))
            SaveDialogs.PickPdbFolder(ui, w);
    }

    private static void ViewMenu(Ui<UiState> ui, Workspace w)
    {
        var t = ui.State.Strings;
        if (ui.MenuItem(t.ViewBack, Shortcuts.Back, enabled: w.History.CanGoBack, id: "back"))
            w.GoBack();
        if (ui.MenuItem(t.ViewForward, Shortcuts.Forward, enabled: w.History.CanGoForward, id: "forward"))
            w.GoForward();
        ui.MenuSeparator();
        DecompilerMenu(ui, w);
        ui.MenuSeparator();
        ui.MenuItem(t.ViewShowNonPublic, ref w.ShowNonPublic, id: "show-non-public");
        ui.MenuSeparator();
        if (ui.MenuItem(t.PanelAssemblies, check: w.Layout.Contains(DockPanels.Assemblies), id: "assemblies"))
            w.OpenAssemblies();
        if (ui.MenuItem(t.ViewBookmarks, check: w.Layout.Contains(DockPanels.Bookmarks), id: "bookmarks"))
            w.OpenBookmarks();
        if (ui.MenuItem(t.PanelAnalyzer, check: w.Layout.Contains(DockPanels.Analyzer), id: "analyzer"))
            w.OpenAnalyzer();
        if (ui.MenuItem(t.PanelSearch, Shortcuts.Search, check: w.Layout.Contains(DockPanels.Search), id: "search"))
            w.OpenSearch();
        if (ui.MenuItem(t.PanelDecompilerOptions, check: w.Layout.Contains(DockPanels.DecompilerOptions), id: "decompiler-options"))
            w.OpenDecompilerOptions();
        if (ui.MenuItem(t.ViewResetLayout, id: "reset-layout"))
            w.AskToResetLayout();
        ui.MenuSeparator();
        ThemeMenu(ui, w);
        LanguageMenu(ui, w);
    }

    private static void DecompilerMenu(Ui<UiState> ui, Workspace w)
    {
        var t = ui.State.Strings;
        using var decompilers = ui.SubMenu(t.ViewDecompiler, id: "decompiler");
        if (!decompilers.IsOpen) return;
        if (ui.MenuRadioItem("C#", w.Language == DecompilationLanguage.CSharp))
            w.SetLanguage(DecompilationLanguage.CSharp);
        if (ui.MenuRadioItem("IL", w.Language == DecompilationLanguage.IL))
            w.SetLanguage(DecompilationLanguage.IL);
    }

    private static void ThemeMenu(Ui<UiState> ui, Workspace w)
    {
        var t = ui.State.Strings;
        using var themes = ui.SubMenu(t.ViewTheme, id: "theme");
        if (!themes.IsOpen) return;
        if (ui.MenuRadioItem(t.ViewThemeDark, w.Theme == AppThemeId.Dark, id: "theme-dark"))
            w.Theme = AppThemeId.Dark;
        if (ui.MenuRadioItem(t.ViewThemeLight, w.Theme == AppThemeId.Light, id: "theme-light"))
            w.Theme = AppThemeId.Light;
        ui.MenuSeparator();
        foreach (var (id, name) in AppThemes.Named)
        {
            if (ui.MenuRadioItem(name, w.Theme == id))
                w.Theme = id;
        }
    }

    private static void LanguageMenu(Ui<UiState> ui, Workspace w)
    {
        var t = ui.State.Strings;
        using var languages = ui.SubMenu(t.ViewLanguage, id: "language");
        if (!languages.IsOpen) return;
        foreach (var (locale, endonym) in Locales.All)
        {
            if (ui.MenuRadioItem(endonym, w.Locale == locale))
                w.SetLocale(locale);
        }
    }
}
