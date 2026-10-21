using ZGF.Gui.Immediate.Widgets;

namespace ZGF.Gui.Immediate.Tests;

public sealed class GlyphRunTests
{
    [Fact]
    public void MeasureCell_answers_the_canvas_cell_for_the_font()
    {
        var frames = new UiFrames();

        var cell = frames.Run(0f, ui => ui.MeasureCell(family: "mono"));

        Assert.Equal(8f, cell.Advance);
        Assert.Equal(16f, cell.Height);
    }

    [Fact]
    public void A_glyph_run_is_drawn_at_the_element_in_canvas_space_with_its_style()
    {
        var frames = new UiFrames();
        int[] codePoints = ['a', 'b', 'c'];

        frames.Run(0f, ui =>
        {
            using var screen = ui.Screen();
            ui.Leaf(new Box { Width = 100f, Height = 20f });
            using (ui.Box(new Box { Width = 100f, Height = 20f }))
                ui.Paint(codePoints, static (p, cps) =>
                    p.GlyphRun(new ZGF.Geometry.PointF(p.Rect.X + 4f, p.Rect.Y), cps, 8f, 0xFF112233, family: "mono", underline: true));
        });

        var run = Assert.Single(frames.Canvas.GlyphRuns);
        Assert.Equal("abc", run.Text);
        Assert.Equal(8f, run.CellAdvance);
        Assert.True(run.Underline);
        Assert.Equal(0xFF112233u, run.Style.TextColor.Value);
        Assert.Equal("mono", run.Style.FontFamily.Value);
        Assert.Equal(16f + 4f, run.Origin.X);
        Assert.Equal(300f - (16f + 20f + 8f), run.Origin.Y);
    }
}
