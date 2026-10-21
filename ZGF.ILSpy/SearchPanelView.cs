using System.Runtime.CompilerServices;
using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;

namespace ZGF.ILSpy;

internal static class SearchPanelView
{
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

        var rowHeight = ui.Theme.RowHeight;
        using (var list = ui.VirtualScroll("results", search.Rows.Count, rowHeight, new Box { Gap = 0f }))
        {
            for (var i = list.First; i < list.End; i++)
            {
                var row = search.Rows[i];
                var node = row.Result.Node;
                using (ui.Id(RuntimeHelpers.GetHashCode(node)))
                {
                    var item = ui.Interact("row");
                    var newTab = item.ClickedWith(PointerButton.Middle);
                    if (item.Clicked || newTab)
                    {
                        search.Selected = i;
                        w.Tree.Reveal(node);
                        w.Show(node, newTab);
                    }

                    var background = i == search.Selected ? theme.Selection : item.Hovered ? theme.Hovered(theme.Background) : 0u;
                    using (ui.Row(item.Id, new Box { Height = rowHeight, Gap = 6f, Padding = new Spacing(8f, 0f), Background = background }))
                    {
                        ui.Text(NodeIcons.Glyph(node.Kind), color: NodeIcons.Color(node.Kind, w.DarkMode), family: NodeIcons.Family);
                        ui.Text(row.Result.Text, color: node.IsPublicApi ? theme.Text : theme.TextMuted);
                        ui.Text(row.Location, color: theme.TextMuted);
                    }
                }
            }
        }
    }
}
