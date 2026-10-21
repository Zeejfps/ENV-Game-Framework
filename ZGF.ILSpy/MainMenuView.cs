using ZGF.Desktop;
using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;

namespace ZGF.ILSpy;

internal static class MainMenuView
{
    private static readonly FileFilter[] AssemblyFilters =
    [
        new("Assemblies", ["*.dll", "*.exe", "*.winmd"]),
        new("All files", ["*.*"]),
    ];

    public static void Render(Ui ui, Workspace w)
    {
        using (ui.MenuBar("menu"))
        {
            using (var file = ui.Menu("File"))
            {
                if (file.IsOpen)
                    FileMenu(ui, w);
            }
            using (var view = ui.Menu("View"))
            {
                if (view.IsOpen)
                    ViewMenu(ui, w);
            }
            using (var navigate = ui.Menu("Navigate"))
            {
                if (navigate.IsOpen)
                    NavigateMenu(ui, w);
            }
        }
    }

    public static void OpenFile(Ui ui, Workspace w) => ui.PickFile("Open assembly", null, AssemblyFilters, w.Open);

    private static void FileMenu(Ui ui, Workspace w)
    {
        if (ui.MenuItem("Open…", "Ctrl+O"))
            OpenFile(ui, w);
        if (ui.MenuItem("Open from framework…"))
            w.Picker.Open(w.Post);
        ui.MenuSeparator();
        var selected = w.Tree.View.HasSelection ? w.Tree.View.Selected!.Assembly : null;
        if (ui.MenuItem("Close", enabled: selected != null))
            w.Close(selected!);
        if (ui.MenuItem("Clear list", enabled: w.Tree.Roots.Count > 0))
            w.ClearList();
        if (ui.MenuItem("Reload all", "F5", enabled: w.Tree.Roots.Count > 0))
            w.Refresh();
        ui.MenuSeparator();
        if (ui.MenuItem("Exit", "Alt+F4"))
            ui.Quit();
    }

    private static void ViewMenu(Ui ui, Workspace w)
    {
        if (ui.MenuItem("C#", check: w.Language == DecompilationLanguage.CSharp))
            w.SetLanguage(DecompilationLanguage.CSharp);
        if (ui.MenuItem("IL", check: w.Language == DecompilationLanguage.IL))
            w.SetLanguage(DecompilationLanguage.IL);
        ui.MenuSeparator();
        ui.MenuItem("Dark mode", ref w.DarkMode);
        ui.MenuItem("Show non-public members", ref w.ShowNonPublic);
    }

    private static void NavigateMenu(Ui ui, Workspace w)
    {
        if (ui.MenuItem("Back", "Alt+Left", enabled: w.History.CanGoBack))
            w.GoBack();
        if (ui.MenuItem("Forward", "Alt+Right", enabled: w.History.CanGoForward))
            w.GoForward();
        ui.MenuSeparator();
        if (ui.MenuItem("Search", "Ctrl+Shift+F"))
            w.OpenSearch();
    }
}
