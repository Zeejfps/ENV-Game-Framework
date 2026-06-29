using ZGF.Gui.Desktop.Components.DataGrid;
using ZGF.Gui.Desktop.Input;
using ZGF.Gui.Testing;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Tests;

public class DataGridNewRowTests
{
    private sealed class Cell { public string Value = ""; }

    private sealed class ListSource : IDataGridSource<Cell>
    {
        public readonly List<Cell> Items;
        public ListSource(List<Cell> items) => Items = items;
        public int Count => Items.Count;
        public bool TryGetItem(int index, out Cell item)
        {
            if (index >= 0 && index < Items.Count) { item = Items[index]; return true; }
            item = null!;
            return false;
        }
        public void EnsureWindow(int first, int last) { }
    }

    [Fact]
    public void CommittingNewRowStrip_AppendsThroughTheAddCallback()
    {
        var items = new List<Cell>();
        var source = new ListSource(items);
        var col = new DataGridColumn<Cell>
        {
            Key = "v",
            Width = ColumnWidth.Flex(),
            Text = c => c.Value,
            CreateEditor = _ => new View(),
            BindEditor = (_, _) => { },
            CommitEditor = (_, item) => item.Value = "stamped",
        };
        var grid = new DataGridView<Cell>(
            new[] { col }, source, DataGridStyle.Default, new FakeCanvas(), new InputSystem(),
            newDraft: () => new Cell(), onAddRow: c => items.Add(c))
        {
            LeftConstraint = 0f, BottomConstraint = 0f, Width = 400f, Height = 200f,
        };
        grid.LayoutSelf();

        // With the source empty, the only row is the trailing new-row strip at index 0.
        grid.BeginEdit(0, 0);
        Assert.True(grid.IsEditing);

        grid.CommitEdit();

        Assert.Single(items);
        Assert.Equal("stamped", items[0].Value);
        Assert.False(grid.IsEditing);
    }

    [Fact]
    public void NewRowStrip_TypeThenEnter_AppendsAndContinuesOnFreshStrip()
    {
        var items = new List<Cell>();
        DataGridView<Cell> grid = null!;
        using var h = GuiTestHarness.Create(ctx =>
        {
            grid = new DataGridView<Cell>(
                new[] { GridColumn.TextEditable<Cell>("v", "V", ColumnWidth.Flex(), c => c.Value, (c, v) => c.Value = v) },
                new ListSource(items), DataGridStyle.Default, ctx.Canvas, ctx.Require<InputSystem>(),
                newDraft: () => new Cell(), onAddRow: c => items.Add(c));
            return grid;
        }, width: 400, height: 200);

        h.Layout();
        var y = grid.Position.Top - DataGridStyle.Default.RowHeight * 0.5f; // row 0 = new-row strip
        var x = grid.Position.Left + 20f;
        h.Click(x, y);
        h.Click(x, y); // double-click begins editing the strip
        h.Type("hello");
        h.PressKey(KeyboardKey.Enter);

        Assert.Single(items);
        Assert.Equal("hello", items[0].Value);
        Assert.True(grid.IsEditing);       // rapid entry: still editing
        Assert.Equal(1, grid.FocusedRow);  // ...the fresh strip, now at index 1
    }
}
