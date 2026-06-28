using ZGF.Geometry;
using ZGF.Gui;
using ZGF.Gui.Desktop.Components.VirtualWidgetList;

namespace ZGF.Gui.Tests;

/// <summary>
/// Geometry + variable-height parity for <see cref="VirtualWidgetListView{TRow}"/> — the same contract
/// <see cref="VirtualRowListTests"/> pins for the immediate-mode list, since the math is ported. Drives
/// the public API directly via <c>LayoutSelf</c> (no harness).
/// </summary>
public class VirtualWidgetListParityTests
{
    private sealed class Row : View;

    private static VirtualWidgetListView<Row> LaidOut(
        float left = 10f, float bottom = 20f, float width = 200f, float height = 100f,
        float rowHeight = 25f, int itemCount = 10, Func<int, float>? heightAt = null)
    {
        var list = new VirtualWidgetListView<Row>
        {
            LeftConstraint = left,
            BottomConstraint = bottom,
            Width = width,
            Height = height,
            RowHeight = rowHeight,
            ItemCount = itemCount,
            RowHeightAt = heightAt,
            CreateRow = () => new Row(),
            BindRow = (_, _) => { },
        };
        list.LayoutSelf();
        return list;
    }

    // --- Uniform geometry -------------------------------------------------------------------------

    [Fact]
    public void TryGetRowRect_FirstRow_SitsAtTopOfViewport()
    {
        var list = LaidOut();

        Assert.True(list.TryGetRowRect(0, out var rect));
        Assert.Equal(10f, rect.Left, 3);
        Assert.Equal(200f, rect.Width, 3);
        Assert.Equal(25f, rect.Height, 3);
        Assert.Equal(list.Position.Top, rect.Top, 3);
    }

    [Fact]
    public void TryGetRowRect_NthRow_StepsDownByRowHeight()
    {
        var list = LaidOut();

        list.TryGetRowRect(3, out var rect);

        Assert.Equal(list.Position.Top - 3 * 25f, rect.Top, 3);
    }

    [Fact]
    public void TryGetRowRect_TracksScroll()
    {
        var list = LaidOut();

        list.TryGetRowRect(2, out var atRest);
        list.SetScrollY(40f);
        list.TryGetRowRect(2, out var scrolled);

        Assert.Equal(atRest.Top + 40f, scrolled.Top, 3);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(10)]
    public void TryGetRowRect_OutOfRange_ReturnsFalse(int index)
    {
        var list = LaidOut(itemCount: 10);

        Assert.False(list.TryGetRowRect(index, out var rect));
        Assert.Equal(default, rect);
    }

    [Fact]
    public void ContentHeight_Uniform_IsItemCountTimesRowHeight()
    {
        Assert.Equal(250f, LaidOut(rowHeight: 25f, itemCount: 10).ContentHeight, 3);
    }

    [Fact]
    public void RowIndexAt_Uniform_MapsPointToRow()
    {
        var list = LaidOut(); // top row spans [top-25, top]

        var top = list.Position.Top;
        Assert.Equal(0, list.RowIndexAt(new PointF(100f, top - 5f)));
        Assert.Equal(2, list.RowIndexAt(new PointF(100f, top - 55f)));
    }

    // --- Variable height (opt-in) -----------------------------------------------------------------
    // Row 2 is "expanded" to 80f, every other row 20f: offsets 0,20,40,120,140,... total 9*20+80 = 260.
    private static float Expanded(int i) => i == 2 ? 80f : 20f;

    [Fact]
    public void ContentHeight_Variable_SumsPerRowHeights()
    {
        Assert.Equal(260f, LaidOut(heightAt: Expanded, itemCount: 10).ContentHeight, 3);
    }

    [Fact]
    public void TryGetRowRect_Variable_StacksByCumulativeOffsets()
    {
        var list = LaidOut(heightAt: Expanded);

        list.TryGetRowRect(0, out var row0);
        list.TryGetRowRect(3, out var row3);

        Assert.Equal(list.Position.Top, row0.Top, 3);
        Assert.Equal(list.Position.Top - 120f, row3.Top, 3); // after rows 0,1 (20) + expanded row 2 (80)
    }

    [Fact]
    public void TryGetRowRect_Variable_ExpandedRowCarriesItsOwnHeight()
    {
        var list = LaidOut(heightAt: Expanded);

        list.TryGetRowRect(1, out var normal);
        list.TryGetRowRect(2, out var expanded);

        Assert.Equal(20f, normal.Height, 3);
        Assert.Equal(80f, expanded.Height, 3);
    }

    [Theory]
    [InlineData(90f, 1)] // GUI-y 90 -> content-y 30, inside the 20f row 1 band [20,40)
    [InlineData(50f, 2)] // GUI-y 50 -> content-y 70, inside the 80f expanded row 2 band [40,120)
    public void RowIndexAt_Variable_MapsPointToBandRow(float y, int expected)
    {
        var list = LaidOut(heightAt: Expanded); // bottom 20 + height 100 -> top at GUI-y 120

        Assert.Equal(expected, list.RowIndexAt(new PointF(100f, y)));
    }

    [Fact]
    public void SetScrollY_Variable_ClampsToContentHeight()
    {
        var list = LaidOut(heightAt: Expanded); // content 260, viewport 100 -> max scroll 160

        list.SetScrollY(1000f);

        Assert.Equal(160f, list.ScrollY, 3);
    }

    [Fact]
    public void EnsureRowVisible_Variable_ScrollsUsingOffsets()
    {
        var list = LaidOut(heightAt: Expanded); // row 5 spans content [160,180]

        list.EnsureRowVisible(5);

        Assert.Equal(80f, list.ScrollY, 3); // 180 (row end) - 100 (viewport)
    }

    [Fact]
    public void InvalidateRowHeights_Variable_RebuildsAfterHeightChange()
    {
        var expanded = 80f;
        var list = LaidOut(heightAt: i => i == 2 ? expanded : 20f);

        Assert.Equal(260f, list.ContentHeight, 3);

        expanded = 40f; // cached until invalidated
        Assert.Equal(260f, list.ContentHeight, 3);

        list.InvalidateRowHeights();
        Assert.Equal(220f, list.ContentHeight, 3);
    }
}
