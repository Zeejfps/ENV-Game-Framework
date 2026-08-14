using ZGF.Gui.Desktop.Components.DataGrid;
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

    // A consumer whose pending entry lives outside the grid (the ledger keeps it in its view model): the draft
    // supplier hands back that same state every time, so flushing into it survives a rebind.
    private sealed class Pending
    {
        public readonly Cell Entry = new();
        public Cell Draft() => Entry;
    }

    private sealed class Fixture
    {
        public required GuiTestHarness H;
        public required DataGridView<Cell> Body;
        public required DataGridNewRowView<Cell> Strip;
        public required List<Cell> Items;

        public void ClickStrip() => H.Click(Strip.Position.Left + 20f, Strip.Position.Bottom + 5f);
        public void ClickBody() => H.Click(Strip.Position.Left + 20f, Strip.Position.Top + 60f);
    }

    private static Fixture Build(int rows = 0, Func<Cell>? newDraft = null)
    {
        var items = new List<Cell>();
        for (var i = 0; i < rows; i++) items.Add(new Cell { Value = i.ToString() });

        DataGridView<Cell> body = null!;
        DataGridNewRowView<Cell> strip = null!;
        var h = GuiTestHarness.Create(ctx => new DataGrid<Cell>
        {
            Columns = new[]
            {
                GridColumn.TextEditable<Cell>("v", "V", ColumnWidth.Flex(), c => c.Value, (c, v) => c.Value = v),
            },
            Source = new ListSource(items),
            NewDraft = newDraft ?? (() => new Cell()),
            OnAddRow = items.Add,
            Ready = v => body = v,
            NewRowReady = v => strip = v,
        }.BuildView(ctx), width: 400, height: 200);
        h.Layout();

        return new Fixture { H = h, Body = body, Strip = strip, Items = items };
    }

    [Fact]
    public void TypeThenEnter_AppendsAndKeepsTheStripFocusedForTheNextEntry()
    {
        var fx = Build();
        using var _ = fx.H;

        fx.ClickStrip();
        Assert.True(fx.Strip.IsEditing);

        fx.H.Type("hello");
        fx.H.PressKey(KeyboardKey.Enter);

        Assert.Equal("hello", Assert.Single(fx.Items).Value);
        Assert.True(fx.Strip.IsEditing); // rapid entry: still on the strip, now blank
    }

    [Fact]
    public void TheStripStaysInPlace_NoMatterHowFarTheBodyIsScrolled()
    {
        var fx = Build(rows: 500);
        using var _ = fx.H;

        var before = fx.Strip.Position;
        Assert.True(before.Height > 0f);

        // Scroll the body to the far end. The strip is not a row of the list, so nothing about it moves.
        fx.H.MoveTo(fx.Strip.Position.Left + 20f, fx.Strip.Position.Top + 60f);
        fx.H.Scroll(0f, -4000f);
        fx.H.Layout();

        Assert.Equal(before, fx.Strip.Position);

        // ...and it's still one click away, with no scrolling back to reach it.
        fx.ClickStrip();
        Assert.True(fx.Strip.IsEditing);
        Assert.Equal(500, fx.Items.Count); // focusing it appended nothing
    }

    [Fact]
    public void ClickingAway_KeepsTheTypedDraft_WithoutAppendingARow()
    {
        var pending = new Pending();
        var fx = Build(rows: 5, newDraft: pending.Draft);
        using var _ = fx.H;

        fx.ClickStrip();
        fx.H.Type("half typed");
        fx.ClickBody(); // losing focus must never insert a half-filled entry

        Assert.False(fx.Strip.IsEditing);
        Assert.Equal(5, fx.Items.Count);
        Assert.Equal("half typed", pending.Entry.Value); // flushed to the consumer's draft, not dropped
    }

    [Fact]
    public void EnterOnTheLastBodyRow_MovesTheEditIntoTheStrip_WithoutInserting()
    {
        var fx = Build(rows: 3);
        using var _ = fx.H;

        fx.Body.BeginEdit(2, 0); // the last row
        Assert.True(fx.Body.IsEditing);

        fx.H.PressKey(KeyboardKey.Enter); // down off the end

        Assert.False(fx.Body.IsEditing);
        Assert.True(fx.Strip.IsEditing);
        Assert.Equal(3, fx.Items.Count); // moving the focus is not an insert
    }

    [Fact]
    public void UpFromTheStrip_MovesTheEditBackIntoTheLastBodyRow()
    {
        var fx = Build(rows: 3);
        using var _ = fx.H;

        fx.ClickStrip();
        Assert.True(fx.Strip.IsEditing);

        fx.H.PressKey(KeyboardKey.UpArrow);

        Assert.False(fx.Strip.IsEditing);
        Assert.True(fx.Body.IsEditing);
        Assert.Equal(2, fx.Body.FocusedRow);
    }
}
