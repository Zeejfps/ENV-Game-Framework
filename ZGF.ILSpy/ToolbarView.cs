using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Localization;
using ZGF.KeyboardModule;

namespace ZGF.ILSpy;

internal static class ToolbarView
{
    private const string SearchShortcut = "Ctrl+Shift+F";

    private static Strings? _languagesFor;
    private static Segment<DecompilationLanguage>[] _languages = [];

    public static void Render(Ui ui, Workspace w, Strings t)
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
                using (var back = ui.SplitIconButton("back", LucideIcons.ArrowLeft, LucideIcons.Family,
                           ui.Memo("back-tip", t, static s => s.ToolbarBack("Alt+Left")), t.ToolbarBackHistory, w.History.CanGoBack, w.History.Revision))
                {
                    if (back.Clicked)
                        w.GoBack();
                    if (back.IsOpen)
                        HistoryItems(ui, w, -1);
                }
                using (var forward = ui.SplitIconButton("forward", LucideIcons.ArrowRight, LucideIcons.Family,
                           ui.Memo("forward-tip", t, static s => s.ToolbarForward("Alt+Right")), t.ToolbarForwardHistory, w.History.CanGoForward,
                           w.History.Revision))
                {
                    if (forward.Clicked)
                        w.GoForward();
                    if (forward.IsOpen)
                        HistoryItems(ui, w, 1);
                }
            }

            using (Group(ui))
            {
                var language = w.Language;
                if (ui.SegmentedControl("language", ref language, Languages(t)))
                    w.SetLanguage(language);
            }

            using (ui.Box(new Box { Width = Sizing.Grow(), Height = Sizing.Grow(), AlignX = Align.End, AlignY = Align.Center }))
                Search(ui, w, t);
        }
    }

    private static Segment<DecompilationLanguage>[] Languages(Strings t)
    {
        if (ReferenceEquals(t, _languagesFor)) return _languages;
        _languagesFor = t;
        _languages =
        [
            new(DecompilationLanguage.CSharp, "C#", t.ToolbarDecompileCsharp),
            new(DecompilationLanguage.IL, "IL", t.ToolbarDisassembleIl),
        ];
        return _languages;
    }

    private static Ui.Scope Group(Ui ui) =>
        ui.Row(new Box { Width = Sizing.Fit(), Gap = ui.Theme.Gap, AlignY = Align.Center });

    private static void HistoryItems(Ui ui, Workspace w, int direction)
    {
        var entries = w.History.Entries;
        var palette = ui.Get<NodeIconPalette>() ?? NodeIconPalette.Dark;
        for (var i = w.History.CurrentIndex + direction; i >= 0 && i < entries.Count; i += direction)
        {
            using (ui.Id(i))
            {
                var entry = entries[i];
                if (ui.MenuItem(entry.Place.Text, id: "entry", icon: NodeIcons.Icon(entry.Place.Kind, entry.Accessibility, palette)))
                {
                    w.GoTo(i);
                    return;
                }
            }
        }
    }

    private static void Search(Ui ui, Workspace w, Strings t)
    {
        var searchId = ui.GetId("search");
        if (w.TakeSearchFocus())
            ui.Focus(searchId);
        var submitted = ui.TextField("search", ref w.Search.Query, t.ToolbarSearch, Sizing.Grow(AppLayout.SearchMinWidth, AppLayout.SearchWidth),
            LucideIcons.Search, LucideIcons.Family, SearchShortcut);
        if (w.Search.Query.Length > 0)
            w.Search.IsOpen = true;
        if (ui.HasFocus(searchId))
            SearchFieldKeys(ui, w, submitted);
        ui.Tooltip(searchId, ui.Memo("search-tip", t, static s => s.ToolbarSearchTooltip(SearchShortcut)));
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
