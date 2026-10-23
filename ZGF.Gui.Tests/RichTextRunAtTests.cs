using Xunit;
using ZGF.Gui.Testing;

namespace ZGF.Gui.Tests;

public sealed class RichTextRunAtTests
{
    private const float Advance = 8f;
    private const float LineHeight = 16f;

    [Fact]
    public void A_point_finds_the_run_of_the_slice_under_it_on_its_line()
    {
        var runs = new[] { new RichTextRun("see ", new TextStyle()), new RichTextRun("List", new TextStyle(), Link: "list"), new RichTextRun(" here", new TextStyle()) };
        var layout = RichTextLayout.Layout(new RecordingCanvas(new SyntheticTextMeasurer()), runs, 9 * Advance);

        Assert.Equal(0, RichTextLayout.RunAt(layout, 1f, 1f));
        Assert.Equal(1, RichTextLayout.RunAt(layout, 5 * Advance, 1f));
        Assert.Equal(2, RichTextLayout.RunAt(layout, 1f, LineHeight + 1f));
    }

    [Fact]
    public void A_point_past_a_line_or_below_the_text_finds_no_run()
    {
        var runs = new[] { new RichTextRun("short", new TextStyle()) };
        var layout = RichTextLayout.Layout(new RecordingCanvas(new SyntheticTextMeasurer()), runs, 0f);

        Assert.Equal(-1, RichTextLayout.RunAt(layout, 6 * Advance, 1f));
        Assert.Equal(-1, RichTextLayout.RunAt(layout, 1f, LineHeight + 1f));
    }
}
