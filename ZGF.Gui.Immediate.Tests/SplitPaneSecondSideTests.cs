using ZGF.Geometry;
using ZGF.Gui.Immediate.Widgets;

namespace ZGF.Gui.Immediate.Tests;

public sealed class SplitPaneSecondSideTests
{
    private float _size = 100f;
    private float _height = 200f;
    private readonly UiFrames _frames;

    public SplitPaneSecondSideTests() => _frames = new UiFrames(body: Body);

    private void Body(Ui ui)
    {
        using (ui.Column(new Box { Height = _height }))
        using (var split = ui.SplitPane("split", Direction.Vertical, ref _size, minFirst: 50f, minSecond: 40f, sized: SplitSide.Second))
        {
            using (split.First())
                ui.Leaf(ui.GetId("top"), new Box { Width = Sizing.Grow(), Height = Sizing.Grow() });
            using (split.Second())
                ui.Leaf(ui.GetId("bottom"), new Box { Width = Sizing.Grow(), Height = Sizing.Grow() });
        }
    }

    [Fact]
    public void The_second_pane_takes_the_apps_size_and_the_first_the_rest()
    {
        _frames.Frame();

        Assert.Equal(100f, _frames.RectOf("bottom").Height);
        Assert.Equal(100f - 6f, _frames.RectOf("top").Height);
    }

    [Fact]
    public void The_second_pane_keeps_its_size_when_the_container_shrinks()
    {
        _frames.Frame();
        _height = 180f;
        _frames.Frame();

        Assert.Equal(100f, _frames.RectOf("bottom").Height);
        Assert.Equal(80f - 6f, _frames.RectOf("top").Height);
    }

    [Theory]
    [InlineData(-30f, 130f)]
    [InlineData(-1000f, 200f - 6f - 50f)]
    [InlineData(1000f, 40f)]
    public void Dragging_the_divider_writes_the_second_panes_size_within_the_minimums(float by, float expected)
    {
        _frames.Frame();
        var divider = _frames.RectOf("top").Bottom + 2.5f;
        _frames.Drag(new PointF(100f, divider), new PointF(100f, divider + by)).Release().Frame();

        Assert.Equal(expected, _size);
    }
}
