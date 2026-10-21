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
        using (ui.MenuBar("menu"))
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
            using (var navigate = ui.Menu(t.MenuNavigate, id: "navigate"))
            {
                if (navigate.IsOpen)
                    NavigateMenu(ui, w, t);
            }
        }
    }

    public static void OpenFile(Ui ui, Workspace w)
    {
        var t = Strings.For(w.Locale);
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
            w.Picker.Open(w.Post);
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
        DecompilerMenu(ui, w, t);
        ui.MenuSeparator();
        ui.MenuItem(t.ViewShowNonPublic, ref w.ShowNonPublic, id: "show-non-public");
        ui.MenuSeparator();
        if (ui.MenuItem(t.PanelAssemblies, check: w.Layout.Contains(DockPanels.Assemblies), id: "assemblies"))
            w.OpenAssemblies();
        if (ui.MenuItem(t.ViewBookmarks, check: w.Layout.Contains(DockPanels.Bookmarks), id: "bookmarks"))
            w.OpenBookmarks();
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
        if (ui.MenuRadioItem(t.ViewThemeDark, w.DarkMode, id: "theme-dark"))
            w.DarkMode = true;
        if (ui.MenuRadioItem(t.ViewThemeLight, !w.DarkMode, id: "theme-light"))
            w.DarkMode = false;
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

    private static void NavigateMenu(Ui ui, Workspace w, Strings t)
    {
        if (ui.MenuItem(t.NavigateBack, "Alt+Left", enabled: w.History.CanGoBack, id: "back"))
            w.GoBack();
        if (ui.MenuItem(t.NavigateForward, "Alt+Right", enabled: w.History.CanGoForward, id: "forward"))
            w.GoForward();
        ui.MenuSeparator();
        if (ui.MenuItem(t.NavigateSearch, "Ctrl+Shift+F", id: "search"))
            w.OpenSearch();
    }
}
