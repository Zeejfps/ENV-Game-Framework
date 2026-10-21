using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;

namespace ZGF.ILSpy;

internal static class ToolbarView
{
    public static void Render(Ui ui, Workspace w)
    {
        var theme = ui.Theme;
        using (ui.Row(new Box { Padding = new Spacing(6f, 4f), Gap = 4f, Background = theme.Surface }))
        {
            if (ui.Button("←", enabled: w.History.CanGoBack, id: "back"))
                w.GoBack();
            ui.Tooltip(ui.GetId("back"), "Back (Alt+Left)");
            if (ui.Button("→", enabled: w.History.CanGoForward, id: "forward"))
                w.GoForward();
            ui.Tooltip(ui.GetId("forward"), "Forward (Alt+Right)");

            ui.Spacer(8f);
            if (ui.ToggleButton("C#", w.Language == DecompilationLanguage.CSharp, "csharp"))
                w.SetLanguage(DecompilationLanguage.CSharp);
            if (ui.ToggleButton("IL", w.Language == DecompilationLanguage.IL, "il"))
                w.SetLanguage(DecompilationLanguage.IL);

            ui.Spacer();
            if (w.FocusSearch)
            {
                w.FocusSearch = false;
                ui.Focus(ui.GetId("search"));
            }
            ui.TextField("search", ref w.Search.Query, "Search types and members (Ctrl+Shift+F)", width: 380f);
            if (w.Search.Query.Length > 0)
                w.Search.IsOpen = true;
        }
    }
}
