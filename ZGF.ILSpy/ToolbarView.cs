using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;
using ZGF.KeyboardModule;

namespace ZGF.ILSpy;

internal static class ToolbarView
{
    private static readonly Segment<DecompilationLanguage>[] Languages =
    [
        new(DecompilationLanguage.CSharp, "C#", "Decompile to C#"),
        new(DecompilationLanguage.IL, "IL", "Disassemble to IL"),
    ];

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
            var language = w.Language;
            if (ui.SegmentedControl("language", ref language, Languages))
                w.SetLanguage(language);

            ui.Spacer();
            var searchId = ui.GetId("search");
            if (w.TakeSearchFocus())
                ui.Focus(searchId);
            var submitted = ui.TextField("search", ref w.Search.Query, "Search types and members (Ctrl+Shift+F)", width: 380f);
            if (w.Search.Query.Length > 0)
                w.Search.IsOpen = true;
            if (ui.HasFocus(searchId))
                SearchFieldKeys(ui, w, submitted);
        }
    }

    private static void SearchFieldKeys(Ui ui, Workspace w, bool submitted)
    {
        var search = w.Search;
        if (submitted)
        {
            if (search.Selected < 0)
                search.Move(1);
            if (search.SelectedRow is { } row)
                w.Show(row.Result.Node);
            return;
        }
        if (ui.Input.IsKeyPressed(KeyboardKey.DownArrow) && search.Rows.Count > 0)
            ui.Focus(SearchPanelView.ResultsId, takesText: false);
    }
}
