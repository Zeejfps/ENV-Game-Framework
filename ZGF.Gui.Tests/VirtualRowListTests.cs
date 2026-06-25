using ZGF.Gui.Desktop.Components.VirtualRowList;
using ZGF.Geometry;

namespace ZGF.Gui.Tests;

public class VirtualRowListTests
{
    private static VirtualRowListView LaidOutList(
        float left = 10f, float bottom = 20f, float width = 200f, float height = 100f,
        float rowHeight = 25f, int itemCount = 10)
    {
        var list = new VirtualRowListView
        {
            LeftConstraint = left,
            BottomConstraint = bottom,
            Width = width,
            Height = height,
            RowHeight = rowHeight,
            ItemCount = itemCount,
        };
        list.LayoutSelf();
        return list;
    }

    [Fact]
    public void TryGetRowRect_FirstRow_SitsAtTopOfViewport()
    {
        var list = LaidOutList();

        var ok = list.TryGetRowRect(0, out var rect);

        Assert.True(ok);
        Assert.Equal(10f, rect.Left, 3);
        Assert.Equal(200f, rect.Width, 3);
        Assert.Equal(25f, rect.Height, 3);
        Assert.Equal(list.Position.Top, rect.Top, 3);
        Assert.Equal(list.Position.Top - 25f, rect.Bottom, 3);
    }

    [Fact]
    public void TryGetRowRect_NthRow_StepsDownByRowHeight()
    {
        var list = LaidOutList();

        list.TryGetRowRect(3, out var rect);

        Assert.Equal(list.Position.Top - 3 * 25f, rect.Top, 3);
    }

    [Fact]
    public void TryGetRowRect_TracksScroll()
    {
        var list = LaidOutList();

        list.TryGetRowRect(2, out var atRest);
        list.SetScrollY(40f);
        list.TryGetRowRect(2, out var scrolled);

        Assert.Equal(atRest.Top + 40f, scrolled.Top, 3);
    }

    [Fact]
    public void TryGetRowRect_RowScrolledAboveViewport_StillReturnsTrueWithGeometry()
    {
        var list = LaidOutList();
        list.SetScrollY(80f);

        var ok = list.TryGetRowRect(0, out var rect);

        Assert.True(ok);
        Assert.True(rect.Bottom > list.Position.Top);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(10)]
    [InlineData(99)]
    public void TryGetRowRect_OutOfRange_ReturnsFalseAndDefault(int index)
    {
        var list = LaidOutList(itemCount: 10);

        var ok = list.TryGetRowRect(index, out var rect);

        Assert.False(ok);
        Assert.Equal(default, rect);
    }

    [Fact]
    public void TryGetRowRect_EmptyList_ReturnsFalse()
    {
        var list = LaidOutList(itemCount: 0);

        Assert.False(list.TryGetRowRect(0, out _));
    }

    // --- Variable-height (opt-in) mode -------------------------------------------------------
    // A row 2 that is "expanded" to 80f while every other row is 20f; the rest of the surface
    // stays the same. offsets = 0,20,40,120,140,160,... and total height = 9*20 + 80 = 260.
    private static VirtualRowListView VariableList(
        Func<int, float> heightAt, int itemCount = 10,
        float left = 10f, float bottom = 20f, float width = 200f, float height = 100f)
    {
        var list = new VirtualRowListView
        {
            LeftConstraint = left,
            BottomConstraint = bottom,
            Width = width,
            Height = height,
            ItemCount = itemCount,
            RowHeightAt = heightAt,
        };
        list.LayoutSelf();
        return list;
    }

    private static float Expanded(int i) => i == 2 ? 80f : 20f;

    [Fact]
    public void ContentHeight_Uniform_IsItemCountTimesRowHeight()
    {
        var list = LaidOutList(rowHeight: 25f, itemCount: 10);

        Assert.Equal(250f, list.ContentHeight, 3);
    }

    [Fact]
    public void ContentHeight_Variable_SumsPerRowHeights()
    {
        var list = VariableList(Expanded, itemCount: 10);

        Assert.Equal(260f, list.ContentHeight, 3);
    }

    [Fact]
    public void TryGetRowRect_Variable_StacksByCumulativeOffsets()
    {
        var list = VariableList(Expanded);

        list.TryGetRowRect(0, out var row0);
        list.TryGetRowRect(3, out var row3);

        Assert.Equal(list.Position.Top, row0.Top, 3);
        // row 3 starts after rows 0,1 (20 each) + the expanded row 2 (80) = 120 below the top.
        Assert.Equal(list.Position.Top - 120f, row3.Top, 3);
    }

    [Fact]
    public void TryGetRowRect_Variable_ExpandedRowCarriesItsOwnHeight()
    {
        var list = VariableList(Expanded);

        list.TryGetRowRect(1, out var normal);
        list.TryGetRowRect(2, out var expanded);

        Assert.Equal(20f, normal.Height, 3);
        Assert.Equal(80f, expanded.Height, 3);
    }

    [Theory]
    [InlineData(90f, 1)] // inside the 20f row 1 band
    [InlineData(50f, 2)] // inside the 80f expanded row 2 band
    public void RowIndexAt_Variable_MapsPointToBandRow(float y, int expected)
    {
        var list = VariableList(Expanded);

        Assert.Equal(expected, list.RowIndexAt(new PointF(100f, y)));
    }

    [Fact]
    public void RowIndexAt_Variable_BelowLastRow_ReturnsMinusOne()
    {
        // Two 20f rows (content 40) inside a 100-tall viewport leaves empty space below the last
        // row; a click there must miss, exactly as the uniform path reports it.
        var list = VariableList(_ => 20f, itemCount: 2);

        Assert.Equal(-1, list.RowIndexAt(new PointF(100f, 50f)));
    }

    [Fact]
    public void SetScrollY_Variable_ClampsToContentHeight()
    {
        var list = VariableList(Expanded); // content 260, viewport 100 -> max scroll 160

        list.SetScrollY(1000f);

        Assert.Equal(160f, list.ScrollY, 3);
    }

    [Fact]
    public void EnsureRowVisible_Variable_ScrollsUsingOffsets()
    {
        var list = VariableList(Expanded); // row 5 spans content [160,180]

        list.EnsureRowVisible(5);

        Assert.Equal(80f, list.ScrollY, 3); // 180 (row end) - 100 (viewport)
    }

    [Fact]
    public void InvalidateRowHeights_Variable_RebuildsAfterHeightChange()
    {
        var expanded = 80f;
        var list = VariableList(i => i == 2 ? expanded : 20f);

        Assert.Equal(260f, list.ContentHeight, 3);

        // Cached until invalidated: a height change alone (ItemCount unchanged) is not observed.
        expanded = 40f;
        Assert.Equal(260f, list.ContentHeight, 3);

        list.InvalidateRowHeights();
        Assert.Equal(220f, list.ContentHeight, 3);
    }
}
