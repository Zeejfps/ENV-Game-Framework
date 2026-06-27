using ZGF.Gui;
using ZGF.Gui.Views;

namespace ZGF.Gui.Tests;

public class HorizontalScrollViewTests
{
    private static RectView Root(float w, float h, View child)
    {
        var root = new RectView { Width = w, Height = h };
        root.Children.Add(child);
        return root;
    }

    // Content whose intrinsic width is naturalWidth but which sets no Width of its own — so its
    // laid-out width follows the constraint the scroller hands it (stretching to fill a wider
    // viewport). This mirrors the toolbar Row, whose width is the measured sum of its buttons.
    private static View Content(float naturalWidth)
    {
        var flex = new FlexView { Axis = Axis.Horizontal };
        flex.Children.Add(new RectView { Width = naturalWidth, Height = 20f });
        return flex;
    }

    private static void AssertRect(View view, float left, float bottom, float width, float height)
    {
        Assert.Equal(left, view.Position.Left, 3);
        Assert.Equal(bottom, view.Position.Bottom, 3);
        Assert.Equal(width, view.Position.Width, 3);
        Assert.Equal(height, view.Position.Height, 3);
    }

    [Fact]
    public void WideContent_LaidOutAtNaturalWidth_FullViewportHeight()
    {
        var content = Content(300f);
        var scroller = new HorizontalScrollView(content);
        var root = Root(100f, 80f, scroller);

        root.LayoutSelf();

        AssertRect(content, 0f, 0f, 300f, 80f);
    }

    [Fact]
    public void Scroll_ShiftsContentLeft_AndClampsToRange()
    {
        var content = Content(300f);
        var scroller = new HorizontalScrollView(content);
        var root = Root(100f, 80f, scroller);
        root.LayoutSelf();

        scroller.ScrollHorizontal(50f);
        root.LayoutSelf();
        Assert.Equal(-50f, content.Position.Left, 3);

        scroller.ScrollHorizontal(1000f);
        root.LayoutSelf();
        Assert.Equal(-200f, content.Position.Left, 3);

        scroller.ScrollHorizontal(-1000f);
        root.LayoutSelf();
        Assert.Equal(0f, content.Position.Left, 3);
    }

    [Fact]
    public void NarrowContent_StretchesToViewport_AndDoesNotScroll()
    {
        var content = Content(40f);
        var scroller = new HorizontalScrollView(content);
        var root = Root(100f, 80f, scroller);
        root.LayoutSelf();

        AssertRect(content, 0f, 0f, 100f, 80f);

        scroller.ScrollHorizontal(50f);
        root.LayoutSelf();
        Assert.Equal(0f, content.Position.Left, 3);
    }

    [Fact]
    public void ReportsZeroIntrinsicWidth_SoItNeverWidensAncestors()
    {
        var scroller = new HorizontalScrollView(Content(300f));

        Assert.Equal(0f, scroller.MeasureWidth(), 3);
    }

    [Fact]
    public void GrowingViewport_PastContent_SnapsScrollBackToOrigin()
    {
        var content = Content(300f);
        var scroller = new HorizontalScrollView(content);
        var root = Root(100f, 80f, scroller);
        root.LayoutSelf();
        scroller.ScrollHorizontal(200f);
        root.LayoutSelf();
        Assert.Equal(-200f, content.Position.Left, 3);

        root.Width = 400f;
        root.LayoutSelf();
        AssertRect(content, 0f, 0f, 400f, 80f);
    }
}
