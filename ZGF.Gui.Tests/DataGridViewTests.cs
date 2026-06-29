using ZGF.Gui.Desktop.Components.DataGrid;
using ZGF.Gui.Desktop.Input;
using ZGF.Gui.Testing;
using ZGF.Gui.Views;

namespace ZGF.Gui.Tests;

public class DataGridViewTests
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

    // A spy column that records each item bound into it, so a test can see what the recycled rows bound
    // without reaching into the (internal) list/row tree.
    private static DataGridColumn<string>[] SpyColumns(List<string> bound) =>
        new[]
        {
            new DataGridColumn<string>
            {
                Key = "spy",
                Width = ColumnWidth.Flex(),
                CreateCell = _ => new RectView(),
                BindCell = (_, item) => bound.Add(item),
            },
        };

    private static DataGridView<string> Build(IDataGridSource<string> source, List<string> bound,
        float width = 400f, float height = 200f)
    {
        var grid = new DataGridView<string>(
            SpyColumns(bound), source, DataGridStyle.Default, new FakeCanvas(), new InputSystem())
        {
            LeftConstraint = 0f,
            BottomConstraint = 0f,
            Width = width,
            Height = height,
        };
        grid.LayoutSelf();
        return grid;
    }

    [Fact]
    public void BindsVisibleRowsFromSource_AndKeepsThePoolBounded()
    {
        var bound = new List<string>();

        Build(new ListSource(Items(10_000)), bound);

        Assert.Contains("Item 0", bound);
        Assert.DoesNotContain("Item 5000", bound);  // far off-screen, never materialized
        Assert.True(bound.Count < 40, $"pool grew to {bound.Count}");
    }

    [Fact]
    public void SelectOnly_ReplacesSelectionAndSetsAnchor()
    {
        var grid = Build(new ListSource(Items(100)), new List<string>());

        grid.SelectOnly(5);
        Assert.Equal(new[] { 5 }, grid.SelectedIndices.OrderBy(i => i));

        grid.SelectOnly(8);
        Assert.Equal(new[] { 8 }, grid.SelectedIndices.OrderBy(i => i));
    }

    [Fact]
    public void ToggleSelected_AddsThenRemoves()
    {
        var grid = Build(new ListSource(Items(100)), new List<string>());

        grid.SelectOnly(5);
        grid.ToggleSelected(7);
        Assert.Equal(new[] { 5, 7 }, grid.SelectedIndices.OrderBy(i => i));

        grid.ToggleSelected(5);
        Assert.Equal(new[] { 7 }, grid.SelectedIndices.OrderBy(i => i));
    }

    [Fact]
    public void SelectRangeTo_FillsFromAnchorInclusive()
    {
        var grid = Build(new ListSource(Items(100)), new List<string>());

        grid.SelectOnly(2);
        grid.SelectRangeTo(5);

        Assert.Equal(new[] { 2, 3, 4, 5 }, grid.SelectedIndices.OrderBy(i => i));
    }

    [Fact]
    public void SelectionChanged_Fires_AndRefreshDropsOutOfRange()
    {
        var items = Items(1000);
        var grid = Build(new ListSource(items), new List<string>());
        var fired = 0;
        grid.SelectionChanged += () => fired++;

        grid.SelectOnly(900);
        Assert.Equal(1, fired);
        Assert.Contains(900, grid.SelectedIndices);

        items.RemoveRange(10, items.Count - 10); // now only 10 rows
        grid.Refresh();

        Assert.DoesNotContain(900, grid.SelectedIndices);
    }

    [Fact]
    public void PoolStaysBounded_ScrollingThroughManyRows()
    {
        var created = 0;
        var columns = new[]
        {
            new DataGridColumn<string>
            {
                Key = "spy",
                Width = ColumnWidth.Flex(),
                CreateCell = _ => { created++; return new RectView(); },
                BindCell = (_, _) => { },
            },
        };
        var grid = new DataGridView<string>(
            columns, new ListSource(Items(100_000)), DataGridStyle.Default, new FakeCanvas(), new InputSystem())
        {
            LeftConstraint = 0f, BottomConstraint = 0f, Width = 400f, Height = 200f,
        };
        grid.LayoutSelf();

        for (var k = 0; k < 100_000; k += 37)
        {
            grid.EnsureRowVisible(k);
            grid.LayoutSelf();
        }

        // One spy cell is created per pooled row widget; the pool covers the viewport (~7 rows) plus overscan,
        // never the 100k items. A regression that broke recycling would blow this up.
        Assert.True(created < 40, $"row-widget pool grew to {created} over a full scroll");
    }

    [Fact]
    public void ClickingARow_SelectsIt_ThroughRealInput()
    {
        var bound = new List<string>();
        DataGridView<string> grid = null!;
        using var h = GuiTestHarness.Create(ctx =>
        {
            grid = new DataGridView<string>(
                SpyColumns(bound), new ListSource(Items(500)), DataGridStyle.Default,
                ctx.Canvas, ctx.Require<InputSystem>());
            return grid;
        }, width: 400, height: 200);

        h.Layout();
        var rowZeroCenterY = grid.Position.Top - DataGridStyle.Default.RowHeight * 0.5f;
        h.Click(grid.Position.Left + 20f, rowZeroCenterY);

        Assert.Equal(new[] { 0 }, grid.SelectedIndices.OrderBy(i => i));
    }

    private sealed class KeyedSource : IDataGridSource<long>
    {
        public readonly List<long> Items;
        public KeyedSource(List<long> items) => Items = items;
        public int Count => Items.Count;
        public bool TryGetItem(int index, out long item)
        {
            if (index >= 0 && index < Items.Count) { item = Items[index]; return true; }
            item = 0;
            return false;
        }
        public void EnsureWindow(int first, int last) { }
    }

    [Fact]
    public void KeyBasedSelection_SurvivesReordering()
    {
        // Items are their own keys (ids 10..19). Select by key, then reorder the source.
        var source = new KeyedSource(Enumerable.Range(10, 10).Select(i => (long)i).ToList());
        var grid = new DataGridView<long>(
            new[] { GridColumn.Text<long>("v", "V", ColumnWidth.Flex(), v => v.ToString()) },
            source, DataGridStyle.Default, new FakeCanvas(), new InputSystem(), key: v => v)
        {
            LeftConstraint = 0f, BottomConstraint = 0f, Width = 400f, Height = 200f,
        };
        grid.LayoutSelf();

        grid.SelectOnly(2); // selects id 12
        Assert.Contains(12L, grid.SelectedKeys);
        Assert.True(grid.IsSelected(2));

        source.Items.Reverse(); // id 12 is now at index 7
        grid.Refresh();

        Assert.Contains(12L, grid.SelectedKeys); // selection kept by key
        Assert.True(grid.IsSelected(7));         // ...and tracks the row to its new index
        Assert.False(grid.IsSelected(2));
    }

    [Fact]
    public void SetExpanded_GrowsTheRowAndPositionsTheOverlay()
    {
        var grid = Build(new ListSource(Items(100)), new List<string>());
        var overlay = new View();

        grid.SetExpanded(2, overlay, 50f);
        grid.LayoutSelf();

        Assert.Equal(2, grid.ExpandedIndex);
        Assert.True(overlay.IsVisible);
        Assert.Equal(50f, overlay.Position.Height, 1); // the band below the one-line strip

        grid.Collapse();
        Assert.Equal(-1, grid.ExpandedIndex);
    }

    [Fact]
    public void RightClickingARow_RaisesContextRequested()
    {
        var bound = new List<string>();
        DataGridView<string> grid = null!;
        var contextRow = -2;
        using var h = GuiTestHarness.Create(ctx =>
        {
            grid = new DataGridView<string>(
                SpyColumns(bound), new ListSource(Items(500)), DataGridStyle.Default,
                ctx.Canvas, ctx.Require<InputSystem>());
            grid.RowContextRequested += (i, _) => contextRow = i;
            return grid;
        }, width: 400, height: 200);

        h.Layout();
        var y = grid.Position.Top - DataGridStyle.Default.RowHeight * 0.5f;
        h.Click(grid.Position.Left + 20f, y, MouseButton.Right);

        Assert.Equal(0, contextRow);
    }
}
