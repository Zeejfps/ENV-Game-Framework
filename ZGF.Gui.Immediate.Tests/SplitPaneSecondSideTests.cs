using ZGF.Gui.Immediate.Widgets;

namespace ZGF.Gui.Immediate.Tests;

public sealed class SplitPaneSecondSideTests
{
    private float _size = 100f;
    private float _height = 200f;
    private readonly UiFrames _frames = new();

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
        _frames.Run(0f, Body);

        Assert.Equal(100f, _frames.RectOf(Body, ui => ui.GetId("bottom")).Height);
        Assert.Equal(100f - 6f, _frames.RectOf(Body, ui => ui.GetId("top")).Height);
    }

    [Fact]
    public void The_second_pane_keeps_its_size_when_the_container_shrinks()
    {
        _frames.Run(0f, Body);
        _height = 180f;
        _frames.Run(0f, Body);

        Assert.Equal(100f, _frames.RectOf(Body, ui => ui.GetId("bottom")).Height);
        Assert.Equal(80f - 6f, _frames.RectOf(Body, ui => ui.GetId("top")).Height);
    }

    [Theory]
    [InlineData(-30f, 130f)]
    [InlineData(-1000f, 200f - 6f - 50f)]
    [InlineData(1000f, 40f)]
    public void Dragging_the_divider_writes_the_second_panes_size_within_the_minimums(float by, float expected)
    {
        _frames.Run(0f, Body);
        var divider = _frames.RectOf(Body, ui => ui.GetId("top")).Bottom + 2.5f;
        _frames.MovePointer(100f, divider);
        _frames.Run(0f, Body);
        _frames.Press();
        _frames.Run(0f, Body);
        _frames.MovePointer(100f, divider + by);
        _frames.Run(0f, Body);
        _frames.Release();
        _frames.Run(0f, Body);

        Assert.Equal(expected, _size);
    }
}
