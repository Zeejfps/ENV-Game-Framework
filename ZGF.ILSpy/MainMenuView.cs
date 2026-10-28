using ZGF.Desktop;
using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Localization;
using static ZGF.ILSpy.Assemblies;
using static ZGF.ILSpy.Dialogs;
using static ZGF.ILSpy.DocumentPanels;
using static ZGF.ILSpy.Exports;
using static ZGF.ILSpy.Navigation;
using static ZGF.ILSpy.Preferences;
using static ZGF.ILSpy.ScreenLayout;

namespace ZGF.ILSpy;

internal static class MainMenuView
{
    private const string AppIconId = "title-bar-icon";

    /// <summary>The menus: in the window's title bar where the platform allows, unless the system's
    /// title bar was chosen, and else in a bar of their own.</summary>
    public static void Render(Ui<UiState> ui, AppState appState)
    {
        if (IsInTitleBar(ui))
            RenderInTitleBar(ui, appState);
        else
            RenderMenuBar(ui, appState);
    }

    private static bool IsInTitleBar(Ui<UiState> ui)
    {
        if (ui.State.Display.SystemTitleBar)
            return false;
        return ui.CanDrawTitleBar;
    }

    /// <summary>The title bar: the menus, the window's under the app's icon first, then room that moves
    /// the window up to the caption buttons.</summary>
    private static void RenderInTitleBar(Ui<UiState> ui, AppState appState)
    {
        var theme = ui.Theme;
        var bar = new Box { Background = theme.Background, Gap = 0f, Padding = new Spacing(theme.Gap, 0f, 0f, 0f) };
        using (ui.TitleBar("title-bar", bar))
        {
            var menus = new Box
            {
                Width = Sizing.Fit(),
                Height = Sizing.Grow(),
                Padding = new Spacing(theme.GapTight),
                Background = theme.Background,
            };
            using (ui.MenuBar("menu", menus))
            {
                WindowMenu(ui);
                Menus(ui, appState);
            }
            ui.TitleBarDragArea("title-bar-drag");
        }
    }

    /// <summary>The menu under the app's icon: restoring, minimizing, maximizing and closing the window.</summary>
    private static void WindowMenu(Ui<UiState> ui)
    {
        var t = ui.State.Strings;
        using var menu = ui.AppIconMenu(AppIconId);
        if (!menu.IsOpen)
            return;
        var icons = ui.State.Icons;
        var maximized = ui.WindowBounds.Maximized;
        if (ui.MenuItem(t.WindowRestore, enabled: maximized, id: "restore", icon: new UiIcon(icons.WindowRestore)))
            ui.RestoreWindow();
        if (ui.MenuItem(t.WindowMinimize, id: "minimize", icon: new UiIcon(icons.WindowMinimize)))
            ui.MinimizeWindow();
        if (ui.MenuItem(t.WindowMaximize, enabled: !maximized, id: "maximize", icon: new UiIcon(icons.WindowMaximize)))
            ui.MaximizeWindow();
        ui.MenuSeparator();
        if (ui.MenuItem(t.WindowClose, "Alt+F4", id: "close", icon: new UiIcon(icons.WindowClose)))
            ui.Quit();
    }

    private static void RenderMenuBar(Ui<UiState> ui, AppState appState)
    {
        using (ui.MenuBar("menu", new Box { Background = ui.Theme.Background }))
            Menus(ui, appState);
    }

    private static void Menus(Ui<UiState> ui, AppState appState)
    {
        var t = ui.State.Strings;
        using (var file = ui.Menu(t.MenuFile, id: "file"))
        {
            if (file.IsOpen)
                FileMenu(ui, appState);
        }
        using (var view = ui.Menu(t.MenuView, id: "view"))
        {
            if (view.IsOpen)
                ViewMenu(ui, appState);
        }
        using (var help = ui.Menu(t.MenuHelp, id: "help"))
        {
            if (help.IsOpen)
                HelpMenu(ui);
        }
    }

    public static void OpenFile(Ui<UiState> ui, AppState appState)
    {
        var t = ui.State.Strings;
        FileFilter[] filters =
        [
            new(t.FileFilterAssembliesAndPackages, ["*.dll", "*.exe", "*.winmd", "*.wasm", "*.nupkg", "*.zip"]),
            new(t.FileFilterAll, ["*.*"]),
        ];
        ui.PickFile(t.FileOpenTitle, null, filters, path => OpenAssembly(appState, path));
    }

    private static void FileMenu(Ui<UiState> ui, AppState appState)
    {
        var t = ui.State.Strings;
        var icons = ui.State.Icons;
        if (ui.MenuItem(t.FileOpen, Shortcuts.Open, id: "open", icon: new UiIcon(icons.FileOpen)))
            OpenFile(ui, appState);
        if (ui.MenuItem(t.FileOpenFromFramework, id: "open-from-framework", icon: new UiIcon(icons.FileOpenFromFramework)))
            ui.State.Picker.Open();
        if (ui.MenuItem(t.FileOpenFromNuget, id: "open-from-nuget", icon: new UiIcon(icons.FileOpenFromNuGet)))
            ui.State.NuGetPicker.Open();
        ui.MenuSeparator();
        ExportItems(ui, appState);
        ui.MenuSeparator();
        var tree = ui.State.Tree;
        var selected = tree.View.HasSelection ? tree.View.Selected!.Assembly : null;
        var anyOpen = tree.Roots.Count > 0;
        if (ui.MenuItem(t.FileClose, enabled: selected != null, id: "close", icon: new UiIcon(icons.FileClose)))
            CloseAssembly(appState, selected!);
        if (ui.MenuItem(t.FileCloseFailed, enabled: AnyFailedToLoad(appState), id: "close-failed", icon: new UiIcon(icons.FileCloseFailed)))
            CloseFailedAssemblies(appState);
        if (ui.MenuItem(t.FileClearList, enabled: anyOpen, id: "clear-list", icon: new UiIcon(icons.FileClearList)))
            CloseAllAssemblies(appState);
        if (ui.MenuItem(t.FileSortList, enabled: anyOpen, id: "sort-list", icon: new UiIcon(icons.FileSortList)))
            SortAssemblies(appState);
        if (ui.MenuItem(t.FileReloadAll, Shortcuts.ReloadAll, enabled: anyOpen, id: "reload-all", icon: new UiIcon(icons.FileReloadAll)))
            RefreshAssemblies(appState, tree.ExpandedPaths());
    }

    private static void ExportItems(Ui<UiState> ui, AppState appState)
    {
        var t = ui.State.Strings;
        var canSave = CanSaveCode(appState, appState.Tabs.Active?.Node);
        if (ui.MenuItem(t.FileSaveCode, Shortcuts.SaveCode, enabled: canSave, id: "save-code", icon: new UiIcon(ui.State.Icons.FileSaveCode)))
            SaveDialogs.PickActiveCodeFile(ui, appState);
        var anyOpen = ui.State.Tree.Roots.Count > 0;
        if (ui.MenuItem(t.FileExport, Shortcuts.Export, enabled: anyOpen, id: "export", icon: new UiIcon(ui.State.Icons.FileExportProject)))
            ui.State.ExportSetup.Open(appState.Assemblies.Assemblies, FindActiveAssembly(appState));
        if (ui.MenuItem(t.FileGeneratePdbs, enabled: anyOpen, id: "generate-pdbs", icon: new UiIcon(ui.State.Icons.FileGeneratePdb)))
            SaveDialogs.PickPdbFolder(ui, appState);
    }

    private static void ViewMenu(Ui<UiState> ui, AppState appState)
    {
        var t = ui.State.Strings;
        if (ui.MenuItem(t.ViewBack, Shortcuts.Back, enabled: appState.History.CanGoBack, id: "back"))
            GoBack(appState);
        if (ui.MenuItem(t.ViewForward, Shortcuts.Forward, enabled: appState.History.CanGoForward, id: "forward"))
            GoForward(appState);
        ui.MenuSeparator();
        DecompilerMenu(ui, appState);
        ui.MenuSeparator();
        ApiVisibilityMenu(ui, appState);
        ui.MenuSeparator();
        PanelsMenu(ui);
        if (ui.MenuItem(t.ViewCloseAllDocuments, enabled: appState.Tabs.Count > 0, id: "close-all-documents"))
            CloseAllTabs(ui.State);
        if (ui.MenuItem(t.ViewResetLayout, id: "reset-layout"))
            AskToResetLayout(ui.State);
        ui.MenuSeparator();
        ThemeMenu(ui, appState);
        LanguageMenu(ui, appState);
    }

    /// <summary>The submenu of the catalog's themes, built-in ones first, the one shown picked.</summary>
    private static void ThemeMenu(Ui<UiState> ui, AppState appState)
    {
        var t = ui.State.Strings;
        using var themes = ui.SubMenu(t.ViewTheme, id: "theme");
        if (!themes.IsOpen) return;
        var catalog = appState.Themes;
        var names = catalog.ListNames(t.Culture);
        for (var i = 0; i < catalog.Ids.Count; i++)
        {
            var id = catalog.Ids[i];
            if (ui.MenuRadioItem(names[i], appState.Theme == id, id: id))
                SetTheme(appState, id);
        }
    }

    /// <summary>The submenu of which types and members the tree shows, the one shown picked.</summary>
    private static void ApiVisibilityMenu(Ui<UiState> ui, AppState appState)
    {
        var t = ui.State.Strings;
        using var shown = ui.SubMenu(t.ViewShownApi, id: "shown-api");
        if (!shown.IsOpen) return;
        ApiVisibilityItem(ui, appState, t.ViewShownPublicOnly, ApiVisibility.PublicOnly);
        ApiVisibilityItem(ui, appState, t.ViewShownSourceDeclared, ApiVisibility.AllButCompilerGenerated);
        ApiVisibilityItem(ui, appState, t.ViewShownAll, ApiVisibility.All);
    }

    private static void ApiVisibilityItem(Ui<UiState> ui, AppState appState, string label, ApiVisibility visibility)
    {
        var selected = appState.ApiVisibility == visibility;
        if (ui.MenuRadioItem(label, selected))
            SetApiVisibility(appState, visibility);
    }

    /// <summary>The submenu of the views, each checked while open, that opens the one picked.</summary>
    private static void PanelsMenu(Ui<UiState> ui)
    {
        var t = ui.State.Strings;
        var screen = ui.State;
        using var panels = ui.SubMenu(t.ViewPanels, id: "panels");
        if (!panels.IsOpen) return;
        if (PanelItem(ui, t.PanelAssemblies, DockPanels.Assemblies))
            OpenAssembliesPanel(screen);
        if (PanelItem(ui, t.ViewBookmarks, DockPanels.Bookmarks))
            OpenBookmarks(screen);
        if (PanelItem(ui, t.PanelAnalyzer, DockPanels.Analyzer))
            OpenAnalyzer(screen);
        if (PanelItem(ui, t.PanelSearch, DockPanels.Search, Shortcuts.Search))
            OpenSearch(screen);
        if (PanelItem(ui, t.PanelDecompilerOptions, DockPanels.DecompilerOptions))
            OpenDecompilerOptions(screen);
        if (PanelItem(ui, t.PanelDisplayOptions, DockPanels.DisplayOptions))
            OpenDisplayOptions(screen);
        if (PanelItem(ui, t.PanelExports, DockPanels.Exports))
            OpenExports(screen);
    }

    /// <summary>The item that opens <paramref name="panel"/>, checked while it is open, with the
    /// icon its tab shows.</summary>
    private static bool PanelItem(Ui<UiState> ui, string label, string panel, string? shortcut = null)
    {
        var open = ui.State.Layout.Contains(panel);
        return ui.MenuItem(label, shortcut, check: open, id: panel, icon: DockPanels.Icon(ui.State.Icons, panel));
    }

    private static void DecompilerMenu(Ui<UiState> ui, AppState appState)
    {
        var t = ui.State.Strings;
        using var decompilers = ui.SubMenu(t.ViewDecompiler, id: "decompiler");
        if (!decompilers.IsOpen) return;
        foreach (var language in DecompilationLanguages.All)
        {
            if (ui.MenuRadioItem(DecompilationLanguages.Label(language), appState.Language == language))
                SetLanguage(appState, language);
        }
    }

    private static void HelpMenu(Ui<UiState> ui)
    {
        var t = ui.State.Strings;
        if (ui.MenuItem(t.HelpAbout, id: "about"))
            OpenAbout(ui.State);
    }

    private static void LanguageMenu(Ui<UiState> ui, AppState appState)
    {
        var t = ui.State.Strings;
        using var languages = ui.SubMenu(t.ViewLanguage, id: "language");
        if (!languages.IsOpen) return;
        foreach (var (locale, endonym) in Locales.All)
        {
            if (ui.MenuRadioItem(endonym, appState.Locale == locale))
                SetLocale(appState, locale);
        }
    }
}
