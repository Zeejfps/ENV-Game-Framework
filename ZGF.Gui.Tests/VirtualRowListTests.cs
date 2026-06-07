using ZGF.Gui.Desktop.Components.VirtualRowList;

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
}
