using ZGF.Geometry;
using ZGF.Gui.Desktop.Components.DataGrid;

namespace ZGF.Gui.Tests;

public class DataGridColumnsTests
{
    // A ledger-shaped layout: Date(resizable) | Description(flex) | Category(resizable) | Amount(resizable).
    private static DataGridColumns LedgerShaped() => new(
        new[]
        {
            ColumnWidth.Resizable(104f, 56f, 2000f),
            ColumnWidth.Flex(1f),
            ColumnWidth.Resizable(240f, 56f, 2000f),
            ColumnWidth.Resizable(124f, 56f, 2000f),
        },
        gap: 16f,
        edgePadding: 14f);

    private static readonly RectF Row = new(0f, 0f, 1000f, 34f);

    private static RectF[] Resolve(DataGridColumns cols, RectF row)
    {
        var cells = new RectF[cols.Count];
        cols.Resolve(row, cells);
        return cells;
    }

    [Fact]
    public void Resolve_PlacesFixedColumnsAndFlexAbsorbsSlack()
    {
        var cells = Resolve(LedgerShaped(), Row);

        // Date at the left edge inset by padding.
        Assert.Equal(14f, cells[0].Left, 3);
        Assert.Equal(104f, cells[0].Width, 3);
        // Description (flex) absorbs the leftover: inner 972 - gaps 48 - fixed 468 = 456.
        Assert.Equal(134f, cells[1].Left, 3);
        Assert.Equal(456f, cells[1].Width, 3);
        // Category and Amount keep their widths.
        Assert.Equal(606f, cells[2].Left, 3);
        Assert.Equal(240f, cells[2].Width, 3);
        Assert.Equal(124f, cells[3].Width, 3);
        // Amount's right edge lands exactly on the inner-right (pinned to the right).
        Assert.Equal(986f, cells[3].Right, 3);
    }

    [Fact]
    public void Resolve_SplitsSlackBetweenFlexColumnsByWeight()
    {
        var cols = new DataGridColumns(
            new[] { ColumnWidth.Fixed(100f), ColumnWidth.Flex(1f), ColumnWidth.Flex(2f) },
            gap: 16f, edgePadding: 14f);

        var cells = Resolve(cols, Row);

        // slack = 972 - 32 - 100 = 840; split 1:2 -> 280 / 560.
        Assert.Equal(280f, cells[1].Width, 3);
        Assert.Equal(560f, cells[2].Width, 3);
        Assert.Equal(986f, cells[2].Right, 3);
    }

    [Fact]
    public void Resolve_ClampsFlexToZeroWhenFixedColumnsOverflow()
    {
        var cols = new DataGridColumns(
            new[] { ColumnWidth.Fixed(600f), ColumnWidth.Fixed(600f), ColumnWidth.Flex(1f) },
            gap: 16f, edgePadding: 14f);

        var cells = Resolve(cols, Row);

        Assert.Equal(0f, cells[2].Width, 3);
    }

    [Fact]
    public void HitTest_MapsXToColumn_GapsFoldIntoLeftColumn()
    {
        var cols = LedgerShaped();

        Assert.Equal(0, cols.HitTest(Row, 10f));   // left of the first column
        Assert.Equal(0, cols.HitTest(Row, 20f));   // inside Date
        Assert.Equal(0, cols.HitTest(Row, 130f));  // gap after Date -> folds left
        Assert.Equal(1, cols.HitTest(Row, 300f));  // inside Description
        Assert.Equal(1, cols.HitTest(Row, 600f));  // gap after Description -> folds left
        Assert.Equal(2, cols.HitTest(Row, 700f));  // inside Category
        Assert.Equal(3, cols.HitTest(Row, 900f));  // inside Amount
        Assert.Equal(3, cols.HitTest(Row, 5000f)); // past the end -> last column
    }

    [Fact]
    public void BoundaryX_IsCentreOfTheGapAfterTheColumn()
    {
        var cols = LedgerShaped();

        Assert.Equal(126f, cols.BoundaryX(0, Row), 3); // Date.Right 118 + gap/2
        Assert.Equal(598f, cols.BoundaryX(1, Row), 3); // Description.Right 590 + gap/2
        Assert.Equal(854f, cols.BoundaryX(2, Row), 3); // Category.Right 846 + gap/2
    }

    [Fact]
    public void SetWidth_ResizesColumn_AndFlexReabsorbs()
    {
        var cols = LedgerShaped();

        var moved = cols.SetWidth(0, 200f);

        Assert.True(moved);
        var cells = Resolve(cols, Row);
        Assert.Equal(200f, cells[0].Width, 3);
        // Description re-absorbs: 972 - 48 - (200+240+124) = 360.
        Assert.Equal(360f, cells[1].Width, 3);
    }

    [Fact]
    public void SetWidth_ClampsToColumnRange()
    {
        var cols = LedgerShaped();

        cols.SetWidth(0, 10f);
        Assert.Equal(56f, cols.CurrentWidth(0), 3);

        cols.SetWidth(0, 9000f);
        Assert.Equal(2000f, cols.CurrentWidth(0), 3);
    }

    [Fact]
    public void SetWidth_IsNoOpForFixedAndFlexColumns()
    {
        var cols = new DataGridColumns(
            new[] { ColumnWidth.Fixed(100f), ColumnWidth.Flex(1f) });

        Assert.False(cols.SetWidth(0, 300f));
        Assert.False(cols.SetWidth(1, 300f));
        Assert.Equal(100f, cols.CurrentWidth(0), 3);
    }

    [Fact]
    public void MaxResizableWidth_CapsGrowthSoFlexKeepsItsFloor()
    {
        var cols = LedgerShaped();

        // Date is 104, Description (flex) is 456; with a flex floor of 80, Date may grow by 456-80=376 -> 480.
        Assert.Equal(480f, cols.MaxResizableWidth(0, Row, 80f), 3);

        // Dragging Date out to its ceiling leaves Description exactly at the floor.
        cols.SetWidth(0, 480f, cols.MaxResizableWidth(0, Row, 80f));
        var cells = Resolve(cols, Row);
        Assert.Equal(80f, cells[1].Width, 3);
    }

    [Fact]
    public void MaxResizableWidth_WithNoFlexColumn_IsTheColumnsOwnMax()
    {
        var cols = new DataGridColumns(
            new[] { ColumnWidth.Resizable(100f, 56f, 300f), ColumnWidth.Fixed(80f) });

        Assert.Equal(300f, cols.MaxResizableWidth(0, Row, 80f), 3);
    }

    [Fact]
    public void Changed_FiresOnceWhenAResizableColumnMoves()
    {
        var cols = LedgerShaped();
        var fired = 0;
        cols.Changed += () => fired++;

        Assert.True(cols.SetWidth(2, 300f));
        Assert.False(cols.SetWidth(2, 300f)); // same value -> no event

        Assert.Equal(1, fired);
    }
}

public class DataGridResizableExtentTests
{
    [Fact]
    public void Constructor_ClampsInitialValueIntoRange()
    {
        Assert.Equal(180f, new ResizableExtent(50f, 180f, 520f).Value, 3);
        Assert.Equal(520f, new ResizableExtent(900f, 180f, 520f).Value, 3);
        Assert.Equal(264f, new ResizableExtent(264f, 180f, 520f).Value, 3);
    }

    [Fact]
    public void Set_ClampsAndFiresOnlyOnRealChange()
    {
        var extent = new ResizableExtent(264f, 180f, 520f);
        var fired = 0;
        extent.Changed += () => fired++;

        Assert.True(extent.Set(300f));
        Assert.False(extent.Set(300f));
        Assert.True(extent.Set(100f)); // clamps to 180
        Assert.Equal(180f, extent.Value, 3);
        Assert.Equal(2, fired);
    }

    [Fact]
    public void Set_DynamicMaxOverridesCeilingForThatCall()
    {
        var extent = new ResizableExtent(100f, 56f, 2000f);

        extent.Set(400f, max: 200f);

        Assert.Equal(200f, extent.Value, 3);
    }
}
