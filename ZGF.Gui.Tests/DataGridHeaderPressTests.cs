using ZGF.Gui;
using ZGF.Gui.Desktop.Components.DataGrid;
using ZGF.Gui.Desktop.Input;
using ZGF.Gui.Testing;
using ZGF.Gui.Views;

namespace ZGF.Gui.Tests;

// The header-affordance hook (OnHeaderPress): a press on a non-sortable column header raises OnHeaderPress
// (so a consumer can open a filter/menu popover), while a sortable column still routes to OnSort. Fired on
// press and consumed, which is what lets a popover toggle cleanly against its own dismiss-on-outside-press.
public class DataGridHeaderPressTests
{
    private sealed class Cell { public int Index; }

    private sealed class ListSource : IDataGridSource<Cell>
    {
        private readonly List<Cell> _items;
        public ListSource(List<Cell> items) => _items = items;
        public int Count => _items.Count;
        public bool TryGetItem(int index, out Cell item)
        {
            if (index >= 0 && index < _items.Count) { item = _items[index]; return true; }
            item = default!;
            return false;
        }
        public void EnsureWindow(int first, int last) { }
    }

    private static List<Cell> Items(int n) =>
        Enumerable.Range(0, n).Select(i => new Cell { Index = i }).ToList();

    private static DataGridColumn<Cell> Text(string key, string header, ColumnWidth width, bool sortable) =>
        new()
        {
            Key = key,
            Header = header,
            Width = width,
            Sortable = sortable,
            CreateCell = _ => new RectView(),
            BindCell = (_, _) => { },
        };

    private static (GuiTestHarness h, View root) Build(List<string> pressed, List<string> sorted)
    {
        View root = null!;
        var h = GuiTestHarness.Create(ctx =>
        {
            var grid = new DataGrid<Cell>
            {
                Columns = new[]
                {
                    Text("name", "Name", ColumnWidth.Flex(), sortable: false),
                    Text("amount", "Amount", ColumnWidth.Fixed(120f), sortable: true),
                },
                Source = new ListSource(Items(50)),
                OnHeaderPress = pressed.Add,
                OnSort = sorted.Add,
            };
            root = grid.BuildView(ctx);
            return root;
        }, width: 400, height: 200);
        h.Layout();
        return (h, root);
    }

    [Fact]
    public void PressingANonSortableHeader_RaisesOnHeaderPress_NotOnSort()
    {
        var pressed = new List<string>();
        var sorted = new List<string>();
        var (h, root) = Build(pressed, sorted);

        var headerY = root.Position.Top - DataGridStyle.Default.HeaderHeight * 0.5f;
        h.Click(root.Position.Left + 20f, headerY); // the left flex "name" column (not sortable)

        Assert.Equal(new[] { "name" }, pressed);
        Assert.Empty(sorted);
    }

    [Fact]
    public void PressingASortableHeader_RaisesOnSort_NotOnHeaderPress()
    {
        var pressed = new List<string>();
        var sorted = new List<string>();
        var (h, root) = Build(pressed, sorted);

        var headerY = root.Position.Top - DataGridStyle.Default.HeaderHeight * 0.5f;
        // The fixed 120-wide "amount" column sits at the right edge (minus the scrollbar gutter).
        var x = root.Position.Right - DataGridStyle.Default.ScrollbarWidth - 60f;
        h.Click(x, headerY);

        Assert.Equal(new[] { "amount" }, sorted);
        Assert.Empty(pressed);
    }
}
