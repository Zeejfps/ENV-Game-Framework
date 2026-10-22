using ZGF.Desktop;
using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

internal static class MainMenuView
{
    public static void Render(Ui ui, Workspace w, Strings t)
    {
        using (ui.MenuBar("menu", new Box { Background = ui.Theme.Background }))
        {
            using (var file = ui.Menu(t.MenuFile, id: "file"))
            {
                if (file.IsOpen)
                    FileMenu(ui, w, t);
            }
            using (var view = ui.Menu(t.MenuView, id: "view"))
            {
                if (view.IsOpen)
                    ViewMenu(ui, w, t);
            }
        }
    }

    public static void OpenFile(Ui ui, Workspace w)
    {
        var t = w.GetStrings();
        FileFilter[] filters =
        [
            new(t.FileFilterAssemblies, ["*.dll", "*.exe", "*.winmd"]),
            new(t.FileFilterAll, ["*.*"]),
        ];
        ui.PickFile(t.FileOpenTitle, null, filters, w.Open);
    }

    private static void FileMenu(Ui ui, Workspace w, Strings t)
    {
        if (ui.MenuItem(t.FileOpen, "Ctrl+O", id: "open"))
            OpenFile(ui, w);
        if (ui.MenuItem(t.FileOpenFromFramework, id: "open-from-framework"))
            w.Picker.Open();
        ui.MenuSeparator();
        var selected = w.Tree.View.HasSelection ? w.Tree.View.Selected!.Assembly : null;
        if (ui.MenuItem(t.FileClose, enabled: selected != null, id: "close"))
            w.Close(selected!);
        if (ui.MenuItem(t.FileClearList, enabled: w.Tree.Roots.Count > 0, id: "clear-list"))
            w.ClearList();
        if (ui.MenuItem(t.FileReloadAll, "F5", enabled: w.Tree.Roots.Count > 0, id: "reload-all"))
            w.Refresh();
        ui.MenuSeparator();
        if (ui.MenuItem(t.FileExit, "Alt+F4", id: "exit"))
            ui.Quit();
    }

    private static void ViewMenu(Ui ui, Workspace w, Strings t)
    {
        if (ui.MenuItem(t.ViewBack, "Alt+Left", enabled: w.History.CanGoBack, id: "back"))
            w.GoBack();
        if (ui.MenuItem(t.ViewForward, "Alt+Right", enabled: w.History.CanGoForward, id: "forward"))
            w.GoForward();
        ui.MenuSeparator();
        DecompilerMenu(ui, w, t);
        ui.MenuSeparator();
        ui.MenuItem(t.ViewShowNonPublic, ref w.ShowNonPublic, id: "show-non-public");
        ui.MenuSeparator();
        if (ui.MenuItem(t.PanelAssemblies, check: w.Layout.Contains(DockPanels.Assemblies), id: "assemblies"))
            w.OpenAssemblies();
        if (ui.MenuItem(t.ViewBookmarks, check: w.Layout.Contains(DockPanels.Bookmarks), id: "bookmarks"))
            w.OpenBookmarks();
        if (ui.MenuItem(t.PanelAnalyzer, check: w.Layout.Contains(DockPanels.Analyzer), id: "analyzer"))
            w.OpenAnalyzer();
        if (ui.MenuItem(t.PanelSearch, "Ctrl+Shift+F", check: w.Layout.Contains(DockPanels.Search), id: "search"))
            w.OpenSearch();
        if (ui.MenuItem(t.PanelDecompilerOptions, check: w.Layout.Contains(DockPanels.DecompilerOptions), id: "decompiler-options"))
            w.OpenDecompilerOptions();
        if (ui.MenuItem(t.ViewResetLayout, id: "reset-layout"))
            w.AskToResetLayout();
        ui.MenuSeparator();
        ThemeMenu(ui, w, t);
        LanguageMenu(ui, w, t);
    }

    private static void DecompilerMenu(Ui ui, Workspace w, Strings t)
    {
        using var decompilers = ui.SubMenu(t.ViewDecompiler, id: "decompiler");
        if (!decompilers.IsOpen) return;
        if (ui.MenuRadioItem("C#", w.Language == DecompilationLanguage.CSharp))
            w.SetLanguage(DecompilationLanguage.CSharp);
        if (ui.MenuRadioItem("IL", w.Language == DecompilationLanguage.IL))
            w.SetLanguage(DecompilationLanguage.IL);
    }

    private static void ThemeMenu(Ui ui, Workspace w, Strings t)
    {
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

    private static void LanguageMenu(Ui ui, Workspace w, Strings t)
    {
        using var languages = ui.SubMenu(t.ViewLanguage, id: "language");
        if (!languages.IsOpen) return;
        foreach (var (locale, endonym) in Locales.All)
        {
            if (ui.MenuRadioItem(endonym, w.Locale == locale))
                w.SetLocale(locale);
        }
    }
}
