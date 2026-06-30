using ZGF.Gui;
using ZGF.Gui.Desktop.Components.VirtualWidgetList;

namespace ZGF.Gui.Tests;

public class VirtualWidgetListTests
{
    private sealed class FakeRow : View
    {
        public int BoundIndex = -1;
        public int BindCount;
    }

    private static (VirtualWidgetListView<FakeRow> list, List<FakeRow> created) MakeList(
        int itemCount, float rowHeight = 25f, float width = 200f, float height = 100f)
    {
        var created = new List<FakeRow>();
        var list = new VirtualWidgetListView<FakeRow>
        {
            LeftConstraint = 10f,
            BottomConstraint = 20f,
            Width = width,
            Height = height,
            RowHeight = rowHeight,
            ItemCount = itemCount,
            CreateRow = () =>
            {
                var r = new FakeRow();
                created.Add(r);
                return r;
            },
            BindRow = (r, i) =>
            {
                r.BoundIndex = i;
                r.BindCount++;
            },
        };
        list.LayoutSelf();
        return (list, created);
    }

    private static List<int> VisibleBoundIndices(VirtualWidgetListView<FakeRow> list)
    {
        var result = new List<int>();
        for (var i = 0; i < list.ChildCount; i++)
            if (list.ChildAt(i) is FakeRow { IsVisible: true } r)
                result.Add(r.BoundIndex);
        result.Sort();
        return result;
    }

    [Fact]
    public void Pool_StaysBounded_RegardlessOfItemCount()
    {
        // A viewport that fits ~4 rows materializes a handful (viewport + overscan), not 10,000.
        var (list, created) = MakeList(itemCount: 10_000);

        Assert.True(created.Count < 10, $"pool grew to {created.Count}");
        Assert.Equal(created.Count, list.ChildCount);
    }

    [Fact]
    public void VisibleRows_CoverViewportFromTop()
    {
        var (list, _) = MakeList(itemCount: 1000);

        var visible = VisibleBoundIndices(list);

        // Rows 0..3 are on screen (100px / 25px); they must be bound.
        Assert.Contains(0, visible);
        Assert.Contains(3, visible);
        Assert.DoesNotContain(-1, visible);
    }

    [Fact]
    public void Scrolling_RebindsRowsToNewIndices()
    {
        var (list, created) = MakeList(itemCount: 1000);

        list.SetScrollY(2500f); // row 100
        list.LayoutSelf();

        var visible = VisibleBoundIndices(list);
        Assert.Contains(100, visible);
        Assert.Contains(103, visible);
        Assert.True(visible[0] >= 99, $"first visible was {visible[0]}");
        // Still a small pool — rows were recycled, not freshly created per index.
        Assert.True(created.Count < 12, $"pool grew to {created.Count}");
    }

    [Fact]
    public void RefreshRows_RebindsVisibleRows_WithoutChangingItemCount()
    {
        var (list, _) = MakeList(itemCount: 1000);
        var row0 = (FakeRow)list.ChildAt(0);
        var before = row0.BindCount;

        list.RefreshRows();
        list.LayoutSelf();

        Assert.Equal(1000, list.ItemCount);
        Assert.True(row0.BindCount > before, "RefreshRows should force a rebind of visible rows");
    }

    [Fact]
    public void VisibleRange_Uniform_MatchesViewport()
    {
        var (list, _) = MakeList(itemCount: 1000);

        Assert.Equal((0, 4), list.VisibleRange());
    }

    [Fact]
    public void EnsureRowVisible_ScrollsRowIntoView()
    {
        var (list, _) = MakeList(itemCount: 1000);

        list.EnsureRowVisible(50); // row 50 spans [1250,1275]; bring its end to the viewport bottom

        Assert.Equal(1175f, list.ScrollY, 3); // 1275 - 100 (viewport)
    }

    [Fact]
    public void EnsureRowVisible_CalledBeforeLayout_IsDeferredAndAppliedOnFirstLayout()
    {
        var list = new VirtualWidgetListView<FakeRow>
        {
            LeftConstraint = 10f,
            BottomConstraint = 20f,
            Width = 200f,
            Height = 100f,
            RowHeight = 25f,
            ItemCount = 1000,
            CreateRow = () => new FakeRow(),
            BindRow = (r, i) => r.BoundIndex = i,
        };

        // No layout yet → no viewport. The request can't scroll now, but it must not be dropped.
        list.EnsureRowVisible(50);
        Assert.Equal(0f, list.ScrollY, 3);

        list.LayoutSelf(); // first real viewport — the deferred reveal lands here
        Assert.Equal(1175f, list.ScrollY, 3); // row 50 end (1275) - viewport (100)
    }

    [Fact]
    public void NotifyItemsChanged_ClampsScrollIntoNewRange()
    {
        var (list, _) = MakeList(itemCount: 1000);
        list.SetScrollY(2000f);

        list.ItemCount = 3; // content now 75px < 100px viewport -> nothing to scroll
        list.NotifyItemsChanged();

        Assert.Equal(0f, list.ScrollY, 3);
    }

    [Fact]
    public void ShrinkingItemCount_HidesSurplusRows()
    {
        var (list, created) = MakeList(itemCount: 1000);

        list.ItemCount = 2;
        list.NotifyItemsChanged();
        list.LayoutSelf();

        Assert.Equal(new List<int> { 0, 1 }, VisibleBoundIndices(list));
        // The extra pooled rows are parked (hidden), not destroyed.
        Assert.True(created.Count > 2);
        Assert.Contains(created, r => !r.IsVisible);
    }

    [Fact]
    public void ContentHeight_IsItemCountTimesRowHeight()
    {
        var (list, _) = MakeList(itemCount: 10, rowHeight: 25f);

        Assert.Equal(250f, list.ContentHeight, 3);
    }
}
