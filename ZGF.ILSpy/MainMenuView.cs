using ZGF.Desktop;
using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Localization;
using static ZGF.ILSpy.Assemblies;
using static ZGF.ILSpy.Exports;
using static ZGF.ILSpy.Navigation;
using static ZGF.ILSpy.Preferences;
using static ZGF.ILSpy.ScreenLayout;

namespace ZGF.ILSpy;

internal static class MainMenuView
{
    private static readonly UiIcon OpenIcon = new(LucideIcons.FolderOpen, LucideIcons.Family);
    private static readonly UiIcon OpenFromFrameworkIcon = new(LucideIcons.Library, LucideIcons.Family);
    private static readonly UiIcon CloseIcon = new(LucideIcons.X, LucideIcons.Family);
    private static readonly UiIcon ClearListIcon = new(LucideIcons.ListX, LucideIcons.Family);
    private static readonly UiIcon ReloadIcon = new(LucideIcons.RotateCcw, LucideIcons.Family);
    private static readonly UiIcon ExitIcon = new(LucideIcons.LogOut, LucideIcons.Family);

    public static void Render(Ui<UiState> ui, AppState appState)
    {
        var t = ui.State.Strings;
        using (ui.MenuBar("menu", new Box { Background = ui.Theme.Background }))
        {
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
        }
    }

    public static void OpenFile(Ui<UiState> ui, AppState appState)
    {
        var t = ui.State.Strings;
        FileFilter[] filters =
        [
            new(t.FileFilterAssemblies, ["*.dll", "*.exe", "*.winmd"]),
            new(t.FileFilterAll, ["*.*"]),
        ];
        ui.PickFile(t.FileOpenTitle, null, filters, path => OpenAssembly(appState, path));
    }

    private static void FileMenu(Ui<UiState> ui, AppState appState)
    {
        var t = ui.State.Strings;
        if (ui.MenuItem(t.FileOpen, Shortcuts.Open, id: "open", icon: OpenIcon))
            OpenFile(ui, appState);
        if (ui.MenuItem(t.FileOpenFromFramework, id: "open-from-framework", icon: OpenFromFrameworkIcon))
            ui.State.Picker.Open();
        ui.MenuSeparator();
        ExportItems(ui, appState);
        ui.MenuSeparator();
        var tree = ui.State.Tree;
        var selected = tree.View.HasSelection ? tree.View.Selected!.Assembly : null;
        var anyOpen = tree.Roots.Count > 0;
        if (ui.MenuItem(t.FileClose, enabled: selected != null, id: "close", icon: CloseIcon))
            CloseAssembly(appState, selected!);
        if (ui.MenuItem(t.FileClearList, enabled: anyOpen, id: "clear-list", icon: ClearListIcon))
            CloseAllAssemblies(appState);
        if (ui.MenuItem(t.FileReloadAll, Shortcuts.ReloadAll, enabled: anyOpen, id: "reload-all", icon: ReloadIcon))
            RefreshAssemblies(appState, tree.ExpandedPaths());
        ui.MenuSeparator();
        if (ui.MenuItem(t.FileExit, "Alt+F4", id: "exit", icon: ExitIcon))
            ui.Quit();
    }

    private static void ExportItems(Ui<UiState> ui, AppState appState)
    {
        var t = ui.State.Strings;
        var canSave = CanSaveCode(appState, appState.Tabs.Active?.Node);
        if (ui.MenuItem(t.FileSaveCode, Shortcuts.SaveCode, enabled: canSave, id: "save-code", icon: SaveDialogs.SaveCodeIcon))
            SaveDialogs.PickActiveCodeFile(ui, appState);
        var anyOpen = ui.State.Tree.Roots.Count > 0;
        if (ui.MenuItem(t.FileExport, Shortcuts.Export, enabled: anyOpen, id: "export", icon: SaveDialogs.ExportIcon))
            ui.State.ExportSetup.Open(appState.Assemblies.Assemblies, FindActiveAssembly(appState));
        if (ui.MenuItem(t.FileGeneratePdbs, enabled: anyOpen, id: "generate-pdbs", icon: SaveDialogs.PdbIcon))
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
        ui.MenuItem(t.ViewShowNonPublic, ref appState.ShowNonPublic, id: "show-non-public");
        ui.MenuSeparator();
        PanelsMenu(ui);
        if (ui.MenuItem(t.ViewResetLayout, id: "reset-layout"))
            AskToResetLayout(ui.State);
        ui.MenuSeparator();
        LanguageMenu(ui, appState);
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
        return ui.MenuItem(label, shortcut, check: open, id: panel, icon: DockPanels.Icon(panel));
    }

    private static void DecompilerMenu(Ui<UiState> ui, AppState appState)
    {
        var t = ui.State.Strings;
        using var decompilers = ui.SubMenu(t.ViewDecompiler, id: "decompiler");
        if (!decompilers.IsOpen) return;
        if (ui.MenuRadioItem("C#", appState.Language == DecompilationLanguage.CSharp))
            SetLanguage(appState, DecompilationLanguage.CSharp);
        if (ui.MenuRadioItem("IL", appState.Language == DecompilationLanguage.IL))
            SetLanguage(appState, DecompilationLanguage.IL);
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
