using ZGF.Gui.Desktop.Components.DataGrid;
using ZGF.Gui.Desktop.Input;
using ZGF.Gui.Testing;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Tests;

public class DataGridEditLifecycleTests
{
    private sealed class Cell { public int Index; }

    private sealed class ListSource<T> : IDataGridSource<T>
    {
        private readonly List<T> _items;
        public ListSource(List<T> items) => _items = items;
        public int Count => _items.Count;
        public bool TryGetItem(int index, out T item)
        {
            if (index >= 0 && index < _items.Count) { item = _items[index]; return true; }
            item = default!;
            return false;
        }
        public void EnsureWindow(int first, int last) { }
    }

    // An editable column with no real editor widget/controller — enough to exercise the grid's lifecycle
    // (state, bind, commit) without keyboard focus. CommitEditor records which item index was committed.
    private static DataGridColumn<Cell> Editable(List<int> committed, string key = "a") => new()
    {
        Key = key,
        Width = ColumnWidth.Flex(),
        Text = c => c.Index.ToString(),
        CreateEditor = _ => new View(),
        BindEditor = (_, _) => { },
        CommitEditor = (_, item) => committed.Add(item.Index),
    };

    private static DataGridView<Cell> Grid(DataGridColumn<Cell>[] columns, int n = 100)
    {
        var items = Enumerable.Range(0, n).Select(i => new Cell { Index = i }).ToList();
        var grid = new DataGridView<Cell>(
            columns, new ListSource<Cell>(items), DataGridStyle.Default, new FakeCanvas(), new InputSystem())
        {
            LeftConstraint = 0f, BottomConstraint = 0f, Width = 400f, Height = 200f,
        };
        grid.LayoutSelf();
        return grid;
    }

    [Fact]
    public void BeginEdit_SetsEditingStateAndSelectsRow()
    {
        var grid = Grid(new[] { Editable(new List<int>()) });

        grid.BeginEdit(3, 0);

        Assert.True(grid.IsEditing);
        Assert.Equal(3, grid.FocusedRow);
        Assert.Equal(0, grid.FocusedColumn);
        Assert.Equal(new[] { 3 }, grid.SelectedIndices.OrderBy(i => i));
    }

    [Fact]
    public void BeginEdit_OnReadOnlyColumn_ResolvesToNearestEditable()
    {
        var columns = new[]
        {
            GridColumn.Text<Cell>("ro", "RO", ColumnWidth.Flex(), c => c.Index.ToString()),
            Editable(new List<int>(), "edit"),
        };
        var grid = Grid(columns);

        grid.BeginEdit(2, 0); // column 0 is read-only

        Assert.Equal(1, grid.FocusedColumn);
    }

    [Fact]
    public void TryGetFocusedCellRect_ReturnsTheEditedCellWithinItsRow()
    {
        var columns = new[]
        {
            GridColumn.Text<Cell>("ro", "RO", ColumnWidth.Fixed(100f), c => c.Index.ToString()),
            Editable(new List<int>(), "edit"),
        };
        var grid = Grid(columns);

        Assert.False(grid.TryGetFocusedCellRect(out _)); // not editing

        grid.BeginEdit(1, 1);

        Assert.True(grid.TryGetFocusedCellRect(out var rect));
        Assert.True(rect.Width > 0f && rect.Height > 0f);
        // It's the editable (second) column, so it sits to the right of the 100px-wide first column.
        Assert.True(rect.Left >= grid.Position.Left + 100f);
    }

    [Fact]
    public void CommitEdit_CommitsFocusedRow_ThenEnds()
    {
        var committed = new List<int>();
        var grid = Grid(new[] { Editable(committed) });

        grid.BeginEdit(5, 0);
        grid.CommitEdit();

        Assert.Equal(new[] { 5 }, committed);
        Assert.False(grid.IsEditing);
    }

    [Fact]
    public void CancelEdit_DoesNotCommit()
    {
        var committed = new List<int>();
        var grid = Grid(new[] { Editable(committed) });

        grid.BeginEdit(5, 0);
        grid.CancelEdit();

        Assert.Empty(committed);
        Assert.False(grid.IsEditing);
    }

    [Fact]
    public void BeginEdit_WhileEditing_CommitsThePreviousRow()
    {
        var committed = new List<int>();
        var grid = Grid(new[] { Editable(committed) });

        grid.BeginEdit(1, 0);
        grid.BeginEdit(4, 0);

        Assert.Equal(new[] { 1 }, committed); // row 1 committed when row 4 began
        Assert.Equal(4, grid.FocusedRow);
        Assert.True(grid.IsEditing);
    }

    [Fact]
    public void DoubleClickingARow_BeginsEditing_ThroughRealInput()
    {
        DataGridView<Cell> grid = null!;
        using var h = GuiTestHarness.Create(ctx =>
        {
            var items = Enumerable.Range(0, 200).Select(i => new Cell { Index = i }).ToList();
            grid = new DataGridView<Cell>(
                new[] { Editable(new List<int>()) }, new ListSource<Cell>(items),
                DataGridStyle.Default, ctx.Canvas, ctx.Require<InputSystem>());
            return grid;
        }, width: 400, height: 200);

        h.Layout();
        var y = grid.Position.Top - DataGridStyle.Default.RowHeight * 0.5f;
        var x = grid.Position.Left + 20f;
        h.Click(x, y);
        h.Click(x, y);

        Assert.True(grid.IsEditing);
        Assert.Equal(0, grid.FocusedRow);
    }

    private static (GuiTestHarness h, DataGridView<Cell> grid) Harness()
    {
        DataGridView<Cell> grid = null!;
        var h = GuiTestHarness.Create(ctx =>
        {
            var items = Enumerable.Range(0, 200).Select(i => new Cell { Index = i }).ToList();
            grid = new DataGridView<Cell>(
                new[] { Editable(new List<int>()) }, new ListSource<Cell>(items),
                DataGridStyle.Default, ctx.Canvas, ctx.Require<InputSystem>());
            return grid;
        }, width: 400, height: 300);
        h.Layout();
        return (h, grid);
    }

    private static void ClickRow(GuiTestHarness h, DataGridView<Cell> grid, int row)
    {
        var y = grid.Position.Top - (row + 0.5f) * DataGridStyle.Default.RowHeight;
        h.Click(grid.Position.Left + 20f, y);
    }

    [Fact]
    public void ArrowKeys_MoveSelection_ThroughRealInput()
    {
        var (h, grid) = Harness();
        using (h)
        {
            ClickRow(h, grid, 2);
            Assert.Equal(new[] { 2 }, grid.SelectedIndices.OrderBy(i => i));

            h.PressKey(KeyboardKey.DownArrow);
            Assert.Equal(new[] { 3 }, grid.SelectedIndices.OrderBy(i => i));

            h.PressKey(KeyboardKey.UpArrow);
            h.PressKey(KeyboardKey.UpArrow);
            Assert.Equal(new[] { 1 }, grid.SelectedIndices.OrderBy(i => i));
        }
    }

    [Fact]
    public void EnterKey_BeginsEditingCurrentRow_ThroughRealInput()
    {
        var (h, grid) = Harness();
        using (h)
        {
            ClickRow(h, grid, 2);
            h.PressKey(KeyboardKey.Enter);

            Assert.True(grid.IsEditing);
            Assert.Equal(2, grid.FocusedRow);
        }
    }
}
