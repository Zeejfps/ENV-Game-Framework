using ZGF.Geometry;
using ZGF.Gui.Desktop.Components.DataGrid;
using ZGF.Gui.Desktop.Input;
using ZGF.Gui.Testing;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Tests;

// The external-selection seam: a grid whose selection is owned elsewhere (e.g. a store) renders what the
// owner pushes via SetSelectedKeys and reports input through RowClicked / NavKeyPressed instead of mutating
// its own selection. This is what lets the Ledger keep the store authoritative over selection in the cutover.
public class DataGridExternalSelectionTests
{
    private sealed class Cell { public long Id; public int Index; }

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

    private static DataGridColumn<Cell> Col() =>
        GridColumn.Text<Cell>("a", "A", ColumnWidth.Flex(), c => c.Index.ToString());

    private static List<Cell> Items(int n) =>
        Enumerable.Range(0, n).Select(i => new Cell { Id = 1000 + i, Index = i }).ToList();

    [Fact]
    public void SetSelectedKeys_RendersExactlyTheOwnersKeys()
    {
        var grid = new DataGridView<Cell>(
            new[] { Col() }, new ListSource(Items(50)), DataGridStyle.Default,
            new FakeCanvas(), new InputSystem(), key: c => c.Id)
        {
            ExternalSelection = true,
            LeftConstraint = 0f, BottomConstraint = 0f, Width = 300f, Height = 200f,
        };
        grid.LayoutSelf();

        grid.SetSelectedKeys(new long[] { 1003, 1007 });
        Assert.Equal(new long[] { 1003, 1007 }, grid.SelectedKeys.OrderBy(k => k));

        grid.SetSelectedKeys(new long[] { 1042 }); // replaces, never accumulates
        Assert.Equal(new long[] { 1042 }, grid.SelectedKeys);
    }

    [Fact]
    public void ExternalSelection_Click_RaisesRowClicked_WithoutSelfSelecting()
    {
        DataGridView<Cell> grid = null!;
        var clicks = new List<int>();
        using var h = GuiTestHarness.Create(ctx =>
        {
            grid = new DataGridView<Cell>(
                new[] { Col() }, new ListSource(Items(200)), DataGridStyle.Default,
                ctx.Canvas, ctx.Require<InputSystem>(), key: c => c.Id)
            {
                ExternalSelection = true,
            };
            grid.RowClicked += (i, _, _) => clicks.Add(i);
            return grid;
        }, width: 300, height: 200);
        h.Layout();

        var y = grid.Position.Top - DataGridStyle.Default.RowHeight * 2.5f; // row 2
        h.Click(grid.Position.Left + 20f, y);

        Assert.Equal(new[] { 2 }, clicks);
        Assert.Empty(grid.SelectedKeys); // owner decides selection; the grid did not self-select
    }

    [Fact]
    public void ExternalSelection_ArrowKey_RaisesNavKeyPressed()
    {
        DataGridView<Cell> grid = null!;
        var navKeys = new List<KeyboardKey>();
        using var h = GuiTestHarness.Create(ctx =>
        {
            grid = new DataGridView<Cell>(
                new[] { Col() }, new ListSource(Items(200)), DataGridStyle.Default,
                ctx.Canvas, ctx.Require<InputSystem>(), key: c => c.Id)
            {
                ExternalSelection = true,
            };
            grid.NavKeyPressed += navKeys.Add;
            return grid;
        }, width: 300, height: 200);
        h.Layout();

        h.Click(grid.Position.Left + 20f, grid.Position.Top - DataGridStyle.Default.RowHeight * 0.5f); // take focus
        h.PressKey(KeyboardKey.DownArrow);
        h.PressKey(KeyboardKey.UpArrow);

        Assert.Equal(new[] { KeyboardKey.DownArrow, KeyboardKey.UpArrow }, navKeys);
        Assert.Empty(grid.SelectedKeys);
    }

    [Fact]
    public void InternalSelection_StillSelfSelectsOnClick_WhenNotExternal()
    {
        DataGridView<Cell> grid = null!;
        using var h = GuiTestHarness.Create(ctx =>
        {
            grid = new DataGridView<Cell>(
                new[] { Col() }, new ListSource(Items(200)), DataGridStyle.Default,
                ctx.Canvas, ctx.Require<InputSystem>(), key: c => c.Id);
            return grid;
        }, width: 300, height: 200);
        h.Layout();

        h.Click(grid.Position.Left + 20f, grid.Position.Top - DataGridStyle.Default.RowHeight * 3.5f); // row 3
        Assert.Equal(new long[] { 1003 }, grid.SelectedKeys); // default behavior unchanged
    }
}
