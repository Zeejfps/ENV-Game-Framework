using Xunit;
using ZGF.Gui.Testing;

namespace ZGF.Gui.Tests;

public sealed class RichTextCharAtTests
{
    private const float Advance = 8f;
    private const float LineHeight = 16f;

    private static readonly RecordingCanvas Canvas = new(new SyntheticTextMeasurer());

    /// <summary>"see List here" in three runs, wrapped after "see List ", whose space stays on the
    /// first line, so the second starts at 9.</summary>
    private static (RichTextRun[] Runs, RichTextLayoutResult Layout) Wrapped()
    {
        var runs = new[] { new RichTextRun("see ", new TextStyle()), new RichTextRun("List", new TextStyle()), new RichTextRun(" here", new TextStyle()) };
        return (runs, RichTextLayout.Layout(Canvas, runs, 9 * Advance));
    }

    [Fact]
    public void Lines_know_where_they_start_and_end_in_the_joined_text()
    {
        var runs = new[] { new RichTextRun("one\ntwo", new TextStyle()) };
        var layout = RichTextLayout.Layout(Canvas, runs, 0f);

        Assert.Equal([(0, 3), (4, 7)], layout.Lines.Select(l => (l.Start, l.End)));
    }

    [Fact]
    public void A_point_finds_the_nearest_caret_across_runs_and_lines()
    {
        var (runs, layout) = Wrapped();

        Assert.Equal(0, RichTextLayout.CharAt(Canvas, runs, layout, 1f, 1f));
        Assert.Equal(5, RichTextLayout.CharAt(Canvas, runs, layout, 5 * Advance - 1f, 1f));
        Assert.Equal(6, RichTextLayout.CharAt(Canvas, runs, layout, 5 * Advance + 5f, 1f));
        Assert.Equal(11, RichTextLayout.CharAt(Canvas, runs, layout, 2 * Advance + 1f, LineHeight + 1f));
    }

    [Fact]
    public void A_point_beyond_the_text_takes_the_nearest_end()
    {
        var (runs, layout) = Wrapped();

        Assert.Equal(9, RichTextLayout.CharAt(Canvas, runs, layout, 50 * Advance, 1f));
        Assert.Equal(0, RichTextLayout.CharAt(Canvas, runs, layout, -5f, -5f));
        Assert.Equal(13, RichTextLayout.CharAt(Canvas, runs, layout, 50 * Advance, 10 * LineHeight));
    }

    [Fact]
    public void A_caret_sits_as_far_along_its_line_as_the_text_before_it()
    {
        var (runs, layout) = Wrapped();

        Assert.Equal(6 * Advance, RichTextLayout.XOf(Canvas, runs, layout.Lines[0], 6));
        Assert.Equal(2 * Advance, RichTextLayout.XOf(Canvas, runs, layout.Lines[1], 11));
        Assert.Equal(0f, RichTextLayout.XOf(Canvas, runs, layout.Lines[1], 3));
    }
}
