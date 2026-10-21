using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;
using ZGF.KeyboardModule;

namespace ZGF.ILSpy;

internal static class ToolbarView
{
    private const string SearchShortcut = "Ctrl+Shift+F";
    private const string SearchTooltip = $"Search types and members ({SearchShortcut})";

    private static readonly Segment<DecompilationLanguage>[] Languages =
    [
        new(DecompilationLanguage.CSharp, "C#", "Decompile to C#"),
        new(DecompilationLanguage.IL, "IL", "Disassemble to IL"),
    ];

    public static void Render(Ui ui, Workspace w)
    {
        var theme = ui.Theme;
        using (ui.Row(new Box
        {
            Height = theme.ToolbarHeight,
            Padding = new Spacing(theme.ShellPaddingX, theme.ToolbarPaddingY),
            Gap = theme.GroupGap,
            AlignY = Align.Center,
            Background = theme.Surface,
        }))
        {
            using (Group(ui))
            {
                if (ui.IconButton("back", LucideIcons.ArrowLeft, LucideIcons.Family, "Back (Alt+Left)", w.History.CanGoBack))
                    w.GoBack();
                if (ui.IconButton("forward", LucideIcons.ArrowRight, LucideIcons.Family, "Forward (Alt+Right)", w.History.CanGoForward))
                    w.GoForward();
            }

            using (Group(ui))
            {
                var language = w.Language;
                if (ui.SegmentedControl("language", ref language, Languages))
                    w.SetLanguage(language);
            }

            using (ui.Box(new Box { Width = Sizing.Grow(), Height = Sizing.Grow(), AlignX = Align.End, AlignY = Align.Center }))
                Search(ui, w);
        }
    }

    private static Ui.Scope Group(Ui ui) =>
        ui.Row(new Box { Width = Sizing.Fit(), Gap = ui.Theme.GapTight, AlignY = Align.Center });

    private static void Search(Ui ui, Workspace w)
    {
        var searchId = ui.GetId("search");
        if (w.TakeSearchFocus())
            ui.Focus(searchId);
        var submitted = ui.TextField("search", ref w.Search.Query, "Search types and members", Sizing.Grow(AppLayout.SearchMinWidth, AppLayout.SearchWidth),
            LucideIcons.Search, LucideIcons.Family, SearchShortcut);
        if (w.Search.Query.Length > 0)
            w.Search.IsOpen = true;
        if (ui.HasFocus(searchId))
            SearchFieldKeys(ui, w, submitted);
        ui.Tooltip(searchId, SearchTooltip);
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
