using ZGF.Gui.Immediate.Widgets;
using ZGF.Gui.Testing;

namespace ZGF.Gui.Immediate.Tests;

/// <summary>
/// Text styles, line heights and cells are kept while asked for, like widths: a screen that keeps drawing
/// the same text measures it once, and sizes it stopped drawing, as a zoom passes through them, go.
/// </summary>
public sealed class TextStyleAgingTests
{
    private sealed class CountingMeasurer : ITextMeasurer
    {
        public float LineHeight = 16f;
        public int LineHeights;
        public int Cells;

        public float MeasureTextWidth(ReadOnlySpan<char> text, TextStyle style)
        {
            if (text.SequenceEqual("0"))
                Cells++;
            return text.Length * 8f;
        }

        public float MeasureTextPrefix(ReadOnlySpan<char> text, int prefixLength, TextStyle style) =>
            Math.Clamp(prefixLength, 0, text.Length) * 8f;

        public float MeasureTextLineHeight(TextStyle style)
        {
            LineHeights++;
            return LineHeight;
        }
    }

    [Fact]
    public void The_same_choices_in_consecutive_frames_give_the_same_style()
    {
        var frames = new UiFrames(new DefaultUiState());
        var first = frames.Run(0f, ui => ui.StyleText(0xFF00FF00u, 13f));

        var second = frames.Run(0f, ui => ui.StyleText(0xFF00FF00u, 13f));

        Assert.Same(first, second);
    }

    [Fact]
    public void A_style_not_asked_for_in_a_frame_is_made_anew_equal_to_the_old()
    {
        var frames = new UiFrames(new DefaultUiState());
        var first = frames.Run(0f, ui => ui.StyleText(0xFF00FF00u, 13f));
        frames.Run(0f, _ => { });

        var again = frames.Run(0f, ui => ui.StyleText(0xFF00FF00u, 13f));

        Assert.NotSame(first, again);
        Assert.Equal(first, again);
    }

    [Fact]
    public void A_style_kept_from_before_it_aged_out_still_draws_in_its_choices()
    {
        var frames = new UiFrames(new DefaultUiState());
        var kept = frames.Run(0f, ui => ui.StyleText(0xFF00FF00u, 13f));
        frames.Run(0f, _ => { });
        frames.Run(0f, _ => { });

        frames.Run(0f, ui =>
        {
            using var screen = ui.Screen();
            ui.RichText([new RichTextRun("kept", kept)]);
        });

        Assert.Equal(0xFF00FF00u, frames.TextColor("kept"));
    }

    [Fact]
    public void A_line_height_and_a_cell_asked_for_each_frame_are_measured_once()
    {
        var measurer = new CountingMeasurer();
        var frames = new UiFrames(new DefaultUiState(), measurer);

        for (var i = 0; i < 5; i++)
            frames.Run(0f, ui => (ui.LineHeight(13f), ui.MeasureCell(13f)));

        Assert.Equal(1, measurer.Cells);
        Assert.Equal(2, measurer.LineHeights);
    }

    [Fact]
    public void A_line_height_and_a_cell_not_asked_for_in_a_frame_are_measured_again_though_their_style_was()
    {
        var measurer = new CountingMeasurer();
        var frames = new UiFrames(new DefaultUiState(), measurer);
        var style = frames.Run(0f, ui =>
        {
            ui.LineHeight(13f);
            ui.MeasureCell(13f);
            return ui.StyleText(size: 13f);
        });
        frames.Run(0f, ui => ui.StyleText(size: 13f));
        var (cells, lineHeights) = (measurer.Cells, measurer.LineHeights);

        var again = frames.Run(0f, ui =>
        {
            ui.LineHeight(13f);
            ui.MeasureCell(13f);
            return ui.StyleText(size: 13f);
        });

        Assert.Same(style, again);
        Assert.Equal(cells + 1, measurer.Cells);
        Assert.Equal(lineHeights + 2, measurer.LineHeights);
    }

    [Fact]
    public void A_font_change_measures_line_heights_again_and_keeps_the_styles()
    {
        var measurer = new CountingMeasurer();
        var frames = new UiFrames(new DefaultUiState(), measurer);
        var style = frames.Run(0f, ui =>
        {
            ui.LineHeight(13f);
            return ui.StyleText(size: 13f);
        });
        measurer.LineHeight = 20f;

        frames.Run(0f, ui =>
        {
            ui.LineHeight(13f);
            ui.StyleText(size: 13f);
            ui.InvalidateMeasurements();
        });
        var (height, again) = frames.Run(0f, ui => (ui.LineHeight(13f), ui.StyleText(size: 13f)));

        Assert.Equal(20f, height);
        Assert.Same(style, again);
    }

    [Fact]
    public void A_scale_change_measures_cells_again_and_keeps_the_styles()
    {
        var measurer = new CountingMeasurer();
        var frames = new UiFrames(new DefaultUiState(), measurer);
        var style = frames.Run(0f, ui =>
        {
            ui.MeasureCell(13f);
            return ui.StyleText(size: 13f);
        });
        var cells = measurer.Cells;

        frames.Canvas.DpiScale = 2f;
        var again = frames.Run(0f, ui =>
        {
            ui.MeasureCell(13f);
            return ui.StyleText(size: 13f);
        });

        Assert.Equal(cells + 1, measurer.Cells);
        Assert.Same(style, again);
    }

    [Fact]
    public void Sizes_a_zoom_passed_through_are_let_go()
    {
        var measurer = new CountingMeasurer();
        var frames = new UiFrames(new DefaultUiState(), measurer);
        var first = frames.Run(0f, ui =>
        {
            ui.LineHeight(10f);
            return ui.StyleText(size: 10f);
        });
        for (var i = 1; i <= 200; i++)
        {
            var size = 10f + i * 0.05f;
            frames.Run(0f, ui => ui.LineHeight(size));
        }
        var lineHeights = measurer.LineHeights;

        var back = frames.Run(0f, ui =>
        {
            ui.LineHeight(10f);
            return ui.StyleText(size: 10f);
        });

        Assert.NotSame(first, back);
        Assert.Equal(lineHeights + 1, measurer.LineHeights);
    }
}
