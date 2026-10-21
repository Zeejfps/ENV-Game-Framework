namespace ZGF.Gui.Immediate.Tests;

public sealed class VirtualScrollOffsetTests
{
    private const int Count = 1_000;
    private const float RowHeight = 20f;

    [Fact]
    public void The_viewport_is_unknown_until_the_list_is_laid_out()
    {
        var frames = new UiFrames();

        var first = Frame(frames);
        var second = Frame(frames);

        Assert.Equal(0f, first.Viewport);
        Assert.Equal(300f - 32f, second.Viewport);
    }

    [Fact]
    public void ScrollTo_moves_the_list_from_the_next_frame_and_clamps_to_the_content()
    {
        var frames = new UiFrames();
        Frame(frames);
        Frame(frames);

        Frame(frames, list => list.ScrollTo(500f));
        Assert.True(frames.RequestedFrame);
        var scrolled = Frame(frames);
        Frame(frames, list => list.ScrollTo(float.MaxValue));
        var clamped = Frame(frames);

        Assert.Equal(500f, scrolled.Offset);
        Assert.Equal(500 / 20, scrolled.First + 1);
        Assert.Equal(Count * RowHeight - (300f - 32f), clamped.Offset);
    }

    private static (float Offset, float Viewport, int First) Frame(UiFrames frames, Action<Ui.VirtualList>? inside = null) =>
        frames.Run(1f / 60f, ui =>
        {
            using var list = ui.VirtualScroll("rows", Count, RowHeight, new Box { Gap = 0f });
            for (var i = list.First; i < list.End; i++)
                ui.Leaf(ui.GetId(i), new Box { Width = Sizing.Grow(), Height = RowHeight });
            inside?.Invoke(list);
            return (list.Offset, list.Viewport, list.First);
        });
}
