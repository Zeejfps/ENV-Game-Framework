using ZGF.Desktop;
using System.Runtime.CompilerServices;
using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.KeyboardModule;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

internal static class SearchPanelView
{
    /// <summary>The results list's focus and scroll id.</summary>
    public const int ResultsId = 0x53524C54;

    private const string SearchShortcut = "Ctrl+Shift+F";

    public static void Render(Ui ui, Workspace w, Strings t)
    {
        var theme = ui.Theme;
        var search = w.Search;
        using var pane = ui.Column(new Box { Height = Sizing.Grow(), Gap = 0f });
        using (ui.PaneToolbar(new Box { Gap = theme.Gap }))
        {
            Field(ui, w, t);
            ui.Text(ui.Memo("search-count", (search.Rows.Count, search.IsBusy, t), static a => a.IsBusy ? a.t.SearchResultsSoFar(a.Count) : a.t.SearchResults(a.Count)),
                color: theme.TextMuted, size: theme.FontSizeSmall);
        }
        ui.Separator();

        var rowHeight = theme.RowHeight;
        var area = ui.Interact(ResultsId);
        if (area.Pressed)
            ui.Focus(ResultsId, takesText: false);
        var focused = ui.HasFocus(ResultsId);
        if (focused)
            Keys(ui, w, rowHeight);

        using var list = ui.VirtualScroll(ResultsId, search.Rows.Count, rowHeight, new Box { Gap = 0f });
        if (search.ScrollToSelected && list.ScrollIntoView(search.Selected))
            search.ScrollToSelected = false;
        for (var i = list.First; i < list.End; i++)
        {
            var row = search.Rows[i];
            var node = row.Result.Node;
            using (ui.Id(RuntimeHelpers.GetHashCode(node)))
            {
                var item = ui.Interact("row");
                var newTab = item.ClickedWith(PointerButton.Middle);
                if (item.Pressed)
                    ui.Focus(ResultsId, takesText: false);
                if (item.Clicked || newTab)
                {
                    search.Select(i);
                    w.Show(node, newTab);
                }

                var selected = i == search.Selected;
                var background = selected ? focused ? theme.Selection : theme.SelectionInactive : item.Hovered ? theme.RowHover : 0u;
                var label = row.Result.Text;
                using (ui.Row(item.Id, new Box
                {
                    Height = rowHeight,
                    Gap = theme.GapTight,
                    Padding = new Spacing(theme.ShellPaddingX, 0f),
                    Background = background,
                }))
                {
                    NodeIcons.Draw(ui, node.Kind);
                    using (ui.Row(new Box { Gap = theme.Gap, AlignY = Align.Center }))
                    {
                        ui.Text(label, new Box { Width = Sizing.Fit() with { Priority = 1 } },
                            color: !node.IsPublicApi ? theme.TextMuted : selected ? theme.SelectionText : theme.Text, overflow: TextOverflow.Ellipsis);
                        ui.Text(row.Location, new Box { Width = Sizing.Fit() with { Priority = 0 } }, color: theme.TextMuted, size: theme.FontSizeSmall,
                            overflow: TextOverflow.EllipsisMiddle);
                    }
                }
                ui.Tooltip(item.Id, NodeText.Tooltip(node));
            }
        }
    }

    private static void Field(Ui ui, Workspace w, Strings t)
    {
        var search = w.Search;
        var fieldId = ui.GetId("search");
        if (w.TakeSearchFocus())
            ui.Focus(fieldId);
        var submitted = ui.TextField("search", ref search.Query, t.SearchField, Sizing.Grow(AppLayout.SearchMinWidth, AppLayout.SearchWidth),
            LucideIcons.Search, LucideIcons.Family);
        ui.Tooltip(fieldId, ui.Memo("search-tip", t, static s => s.SearchFieldTooltip(SearchShortcut)));
        if (!ui.HasFocus(fieldId)) return;
        if (submitted)
        {
            if (search.Selected < 0)
                search.Move(1);
            if (search.SelectedRow is { } row)
                w.Show(row.Result.Node);
        }
        else if (ui.Input.IsKeyPressed(KeyboardKey.DownArrow) && search.Rows.Count > 0)
        {
            ui.Focus(ResultsId, takesText: false);
        }
    }

    private static void Keys(Ui ui, Workspace w, float rowHeight)
    {
        var search = w.Search;
        var page = ui.TryGetRect(ResultsId, out var view) ? Math.Max(1, (int)(view.Height / rowHeight) - 1) : 10;
        var keys = ui.Input.Keys;
        for (var k = 0; k < keys.Count; k++)
        {
            var stroke = keys[k];
            if ((stroke.Modifiers & (KeyModifiers.Alt | KeyModifiers.Super)) != 0) continue;
            switch (stroke.Key)
            {
                case KeyboardKey.UpArrow:
                    search.Move(-1);
                    break;
                case KeyboardKey.DownArrow:
                    search.Move(1);
                    break;
                case KeyboardKey.PageUp:
                    search.Move(-page);
                    break;
                case KeyboardKey.PageDown:
                    search.Move(page);
                    break;
                case KeyboardKey.Home:
                    search.Move(-search.Rows.Count);
                    break;
                case KeyboardKey.End:
                    search.Move(search.Rows.Count);
                    break;
                case KeyboardKey.Enter when search.SelectedRow is { } selected:
                    w.Show(selected.Result.Node, inNewTab: (stroke.Modifiers & KeyModifiers.Control) != 0);
                    break;
            }
        }
    }
}
