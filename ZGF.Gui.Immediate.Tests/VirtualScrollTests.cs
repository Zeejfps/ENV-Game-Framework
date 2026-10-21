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

    [Fact]
    public void Tall_rows_push_the_rows_after_them_down_and_still_scroll_to_the_end()
    {
        var frames = new UiFrames();
        TallRow[] tall = [new(2, 100f), new(5000, 300f)];
        float x = 0f, y = 0f;
        (int First, int End) Tall(Action<Ui.VirtualList>? inside = null) => frames.Run(1f / 60f, ui =>
        {
            using var list = ui.VirtualScroll(ui.GetId("rows"), Count, RowHeight, ref x, ref y, default, tall);
            for (var i = list.First; i < list.End; i++)
                ui.Leaf(ui.GetId(i), new Box { Width = Sizing.Grow(), Height = i == 2 ? 100f : i == 5000 ? 300f : RowHeight });
            inside?.Invoke(list);
            return (list.First, list.End);
        });
        Tall();
        Tall();

        var third = frames.Run(0f, ui => ui.TryGetRect(ui.GetId(3), out var rect) ? rect.Y : float.NaN);
        Assert.Equal(2 * Stride + 100f + 8f, third, 0.01f);

        Tall(list => list.ScrollIntoView(5001));
        var (first, end) = Tall();
        Tall();
        var (tallRow, after) = frames.Run(0f, ui => (ui.TryGetRect(ui.GetId(5000), out var a) ? a : default, ui.TryGetRect(ui.GetId(5001), out var b) ? b : default));
        Assert.InRange(5000, first, end - 1);
        Assert.Equal(300f, after.Bottom, 0.01f);
        Assert.Equal(300f, tallRow.Height, 0.01f);
        Assert.Equal(after.Y - 8f - 300f, tallRow.Y, 0.01f);

        Tall(list => list.ScrollIntoView(Count - 1));
        Tall();
        var (_, last) = Tall();
        Assert.Equal(Count, last);
        Assert.Equal(300f, frames.Run(0f, ui => ui.TryGetRect(ui.GetId(Count - 1), out var rect) ? rect.Bottom : float.NaN), 0.01f);
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
