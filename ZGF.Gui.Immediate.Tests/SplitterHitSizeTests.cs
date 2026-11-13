using ZGF.Desktop;
using ZGF.Geometry;
using ZGF.Gui.Immediate.Widgets;

namespace ZGF.Gui.Immediate.Tests;

/// <summary>A divider one point thick between its panes, grabbed across nine: four on each side of its line.</summary>
public sealed class SplitterHitSizeTests
{
    private float _size = 100f;
    private readonly UiFrames _frames;

    public SplitterHitSizeTests()
    {
        var state = new DefaultUiState { Theme = UiTheme.Dark with { SplitterSize = 1f, SplitterHitSize = 9f } };
        _frames = new UiFrames(state, body: Body);
    }

    private void Body(Ui ui)
    {
        using var screen = ui.Screen();
        using var split = ui.SplitPane("split", Direction.Horizontal, ref _size, minFirst: 50f, minSecond: 100f);
        using (split.First())
            ui.Leaf(ui.GetId("left"), new Box { Width = Sizing.Grow(), Height = Sizing.Grow() });
        using (split.Second())
            ui.Leaf(ui.GetId("right"), new Box { Width = Sizing.Grow(), Height = Sizing.Grow() });
    }

    [Fact]
    public void The_divider_takes_only_its_strip_between_the_panes()
    {
        _frames.Frame();

        Assert.Equal(100f, _frames.RectOf("left").Width);
        Assert.Equal(368f - 101f, _frames.RectOf("right").Width);
    }

    [Fact]
    public void The_pointer_grabs_it_over_the_pane_beside_its_line()
    {
        var right = _frames.Frame().RectOf("right");
        _frames.MovePointer(right.X + 3f, 150f).Frame();

        Assert.Equal(MouseCursor.ResizeHorizontal, _frames.Frame(ui => ui.Cursor));
    }

    [Fact]
    public void Dragging_from_beside_the_line_moves_it_by_as_much_as_the_pointer()
    {
        var right = _frames.Frame().RectOf("right");
        var from = right.X + 3f;

        _frames.Drag(new PointF(from, 150f), new PointF(from + 100f, 150f)).Release().Frame(2);

        Assert.Equal(200f, _size, 0.01f);
        Assert.Equal(200f, _frames.RectOf("left").Width, 0.01f);
    }
}
