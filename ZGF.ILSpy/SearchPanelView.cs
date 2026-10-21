using ZGF.Desktop;
using System.Runtime.CompilerServices;
using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.KeyboardModule;

namespace ZGF.ILSpy;

internal static class SearchPanelView
{
    /// <summary>The results list's focus and scroll id.</summary>
    public const int ResultsId = 0x53524C54;

    public static void Render(Ui ui, Workspace w)
    {
        var theme = ui.Theme;
        var search = w.Search;
        using (ui.Row(new Box { Height = AppThemes.BarHeight + 4f, Padding = new Spacing(8f, 0f, 4f, 0f), Background = theme.Surface }))
        {
            ui.Text("Search results", bold: true);
            ui.Text(ui.Memo("search-count", (search.Rows.Count, search.IsBusy), static a => a.IsBusy ? $"{a.Count:N0} so far…" : $"{a.Count:N0}"),
                color: theme.TextMuted);
            ui.Spacer();
            if (ui.Button("Close", id: "close-search"))
                search.Clear();
        }

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

                var background = i == search.Selected
                    ? focused ? theme.Selection : UiColor.Mix(theme.Background, theme.Text, 0.12f)
                    : item.Hovered ? theme.Hovered(theme.Background) : 0u;
                using (ui.Row(item.Id, new Box { Height = rowHeight, Gap = 6f, Padding = new Spacing(8f, 0f), Background = background }))
                {
                    ui.Text(NodeIcons.Glyph(node.Kind), color: NodeIcons.Color(node.Kind, w.DarkMode), family: NodeIcons.Family);
                    ui.Text(row.Result.Text, color: node.IsPublicApi ? theme.Text : theme.TextMuted);
                    ui.Text(row.Location, color: theme.TextMuted);
                }
            }
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
