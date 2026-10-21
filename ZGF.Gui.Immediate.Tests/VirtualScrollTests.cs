namespace ZGF.Gui.Immediate.Tests;

public sealed class VirtualScrollTests
{
    private const int Count = 10_000;
    private const float RowHeight = 20f;
    private const float Stride = RowHeight + 8f;

    [Fact]
    public void Only_the_rows_in_view_are_recorded()
    {
        var frames = new UiFrames();
        Frame(frames);

        var (first, end) = Frame(frames);

        Assert.Equal(0, first);
        Assert.Equal((int)MathF.Ceiling(300f / Stride) + 3, end);
    }

    [Fact]
    public void Scrolling_moves_the_range_and_keeps_rows_where_a_full_list_puts_them()
    {
        var frames = new UiFrames();
        Frame(frames);

        frames.MovePointer(100f, 100f);
        Frame(frames);
        frames.Wheel(-100);
        Frame(frames);
        var (first, end) = Frame(frames);

        Assert.Equal((int)(4000f / Stride) - 1, first);
        var top = frames.Run(0f, ui => ui.TryGetRect(ui.GetId(first + 1), out var rect) ? rect.Y : float.NaN);
        Assert.Equal((first + 1) * Stride - 4000f, top, 0.01f);
    }

    [Fact]
    public void Spacers_keep_the_whole_list_scrollable_to_its_last_row()
    {
        var frames = new UiFrames();
        Frame(frames);

        Frame(frames, list => list.ScrollIntoView(Count - 1));
        Frame(frames);
        var (_, end) = Frame(frames);

        Assert.Equal(Count, end);
        var bottom = frames.Run(0f, ui => ui.TryGetRect(ui.GetId(Count - 1), out var rect) ? rect.Bottom : float.NaN);
        Assert.Equal(300f, bottom, 0.01f);
    }

    private static (int First, int End) Frame(UiFrames frames, Action<Ui.VirtualList>? inside = null) =>
        frames.Run(1f / 60f, ui =>
        {
            using var list = ui.VirtualScroll("rows", Count, RowHeight);
            for (var i = list.First; i < list.End; i++)
                ui.Leaf(ui.GetId(i), new Box { Width = Sizing.Grow(), Height = RowHeight });
            inside?.Invoke(list);
            return (list.First, list.End);
        });
}
