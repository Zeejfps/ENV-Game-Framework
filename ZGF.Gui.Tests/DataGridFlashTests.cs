using ZGF.Gui.Desktop.Components.DataGrid;
using ZGF.Gui.Desktop.Input;
using ZGF.Gui.Testing;
using ZGF.Gui.Views;

namespace ZGF.Gui.Tests;

// The transient reveal-highlight (SetFlash): an owner flashes a just-inserted or navigated-to row. The grid
// tracks the flashed index, rebinds when it changes (so the row repaints with the flash background), and is
// idempotent when set to the same value. Selection still takes visual precedence — covered by the row's bind
// order, exercised here only at the index/rebind level since the FakeCanvas doesn't record fills.
public class DataGridFlashTests
{
    private sealed class ListSource : IDataGridSource<string>
    {
        private readonly List<string> _items;
        public ListSource(List<string> items) => _items = items;
        public int Count => _items.Count;
        public bool TryGetItem(int index, out string item)
        {
            if (index >= 0 && index < _items.Count) { item = _items[index]; return true; }
            item = string.Empty;
            return false;
        }
        public void EnsureWindow(int first, int last) { }
    }

    private static List<string> Items(int n) =>
        Enumerable.Range(0, n).Select(i => $"Item {i}").ToList();

    private static DataGridView<string> Build(List<string> bound)
    {
        var columns = new[]
        {
            new DataGridColumn<string>
            {
                Key = "spy",
                Width = ColumnWidth.Flex(),
                CreateCell = _ => new RectView(),
                BindCell = (_, item) => bound.Add(item),
            },
        };
        var grid = new DataGridView<string>(
            columns, new ListSource(Items(100)), DataGridStyle.Default, new FakeCanvas(), new InputSystem())
        {
            LeftConstraint = 0f, BottomConstraint = 0f, Width = 400f, Height = 200f,
        };
        grid.LayoutSelf();
        return grid;
    }

    [Fact]
    public void SetFlash_TracksTheIndex_AndClearsWithNull()
    {
        var grid = Build(new List<string>());
        Assert.Equal(-1, grid.FlashIndex);

        grid.SetFlash(3);
        Assert.Equal(3, grid.FlashIndex);

        grid.SetFlash(null);
        Assert.Equal(-1, grid.FlashIndex);
    }

    [Fact]
    public void SetFlash_RebindsRows_OnlyWhenTheIndexChanges()
    {
        var bound = new List<string>();
        var grid = Build(bound);

        bound.Clear();
        grid.SetFlash(2);
        grid.LayoutSelf();
        Assert.NotEmpty(bound); // changing the flash marks dirty → next layout repaints the visible rows

        bound.Clear();
        grid.SetFlash(2); // same value → no rebind requested
        grid.LayoutSelf();
        Assert.Empty(bound);
    }
}
