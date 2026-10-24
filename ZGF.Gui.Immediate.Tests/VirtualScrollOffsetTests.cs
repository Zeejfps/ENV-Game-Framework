using ZGF.Geometry;
using ZGF.Gui.Immediate.Widgets;

namespace ZGF.Gui.Immediate.Tests;

public sealed class VirtualScrollOffsetTests
{
    private const int Count = 1_000;
    private const float RowHeight = 20f;

    [Fact]
    public void The_viewport_is_unknown_until_the_list_is_laid_out()
    {
        var frames = new UiFrames(new DefaultUiState());

        var first = Frame(frames);
        var second = Frame(frames);

        Assert.Equal(0f, first.Viewport);
        Assert.Equal(300f - 32f, second.Viewport);
    }

    [Fact]
    public void ScrollTo_moves_the_list_from_the_next_frame_and_clamps_to_the_content()
    {
        var frames = new UiFrames(new DefaultUiState());
        Frame(frames);
        Frame(frames);

        Frame(frames, list => list.ScrollTo(500f));
        Assert.True(frames.UpdateRequested);
        var scrolled = Frame(frames);
        Frame(frames, list => list.ScrollTo(float.MaxValue));
        var clamped = Frame(frames);

        Assert.Equal(500f, scrolled.Offset);
        Assert.Equal(500 / 20, scrolled.First + 1);
        Assert.Equal(Count * RowHeight - (300f - 32f), clamped.Offset);
    }

    [Fact]
    public void Dragging_or_wheeling_past_the_end_never_hands_back_an_offset_beyond_it()
    {
        float x = 0f, y = 0f;
        var frames = new UiFrames(new DefaultUiState(), body: ui =>
        {
            using var screen = ui.Screen();
            using var list = ui.VirtualScroll(ui.GetId("wide"), Count, RowHeight, ref x, ref y);
            for (var i = list.First; i < list.End; i++)
                ui.Leaf(new Box { Width = 1000f, Height = RowHeight });
        }).Frame(2);
        var thumb = frames.RectOf(ui => Ui.HorizontalThumbId(ui.GetId("wide"), ScrollAxes.Both));
        var max = 1000f - frames.Run(0f, ui => ui.TryGetContentRect(ui.GetId("wide"), out var rect) ? rect.Width : 0f);

        frames.Drag(new PointF(thumb.X + 2f, thumb.Y + thumb.Height / 2));
        var reported = new List<float>();
        for (var step = 1; step <= 10; step++)
        {
            frames.MovePointer(thumb.X + 2f + step * 200f, thumb.Y + thumb.Height / 2).Frame();
            reported.Add(x);
        }
        frames.Release().Frame();
        frames.MovePointer(100f, 100f).Frame();
        for (var i = 0; i < 5; i++)
        {
            frames.Wheel(0, -3).Frame();
            reported.Add(x);
        }

        Assert.True(max > 0f);
        Assert.All(reported, offset => Assert.InRange(offset, 0f, max + 0.5f));
        Assert.Equal(max, reported[^1], 0.5f);
    }

    private static (float Offset, float Viewport, int First) Frame(UiFrames frames, Action<Ui.VirtualList>? inside = null) =>
        frames.Run(1f / 60f, ui =>
        {
            using var screen = ui.Screen();
            using var list = ui.VirtualScroll("rows", Count, RowHeight);
            for (var i = list.First; i < list.End; i++)
                ui.Leaf(ui.GetId(i), new Box { Width = Sizing.Grow(), Height = RowHeight });
            inside?.Invoke(list);
            return (list.Offset, list.Viewport, list.First);
        });
}
