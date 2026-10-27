using ZGF.Geometry;

namespace ZGF.Gui.Immediate.Tests;

public sealed class PainterCurveTests
{
    private const uint Ink = 0xFF336699;
    private const float Height = 300f;

    private static UiFrames Painted(Action<UiPainter> paint)
    {
        return new UiFrames(new DefaultUiState(), height: Height, body: ui =>
        {
            using (ui.Box(new Box { Width = Sizing.Grow(), Height = Sizing.Grow() }))
                ui.Paint(paint);
        }).Frame();
    }

    /// <summary>Where a point in UI space lands on the canvas, whose Y runs up.</summary>
    private static PointF OnCanvas(float x, float y) => new(x, Height - y);

    [Fact]
    public void A_cubic_bezier_is_drawn_through_its_control_points_on_the_canvas()
    {
        var frames = Painted(p => p.CubicBezier(new PointF(10f, 20f), new PointF(30f, 40f), new PointF(50f, 60f), new PointF(70f, 80f), 2f, Ink));

        var curve = Assert.Single(frames.Canvas.CubicBeziers).Inputs;
        Assert.Equal(OnCanvas(10f, 20f), curve.Start);
        Assert.Equal(OnCanvas(30f, 40f), curve.Control1);
        Assert.Equal(OnCanvas(50f, 60f), curve.Control2);
        Assert.Equal(OnCanvas(70f, 80f), curve.End);
        Assert.Equal(2f, curve.Thickness);
        Assert.Equal(Ink, curve.Color);
        Assert.Equal(0f, curve.DashLength);
    }

    [Fact]
    public void A_dashed_cubic_bezier_carries_its_dashes_and_gaps()
    {
        var frames = Painted(p => p.CubicBezier(new PointF(0f, 0f), new PointF(1f, 1f), new PointF(2f, 2f), new PointF(3f, 3f), 1f, Ink, 6f, 4f));

        var curve = Assert.Single(frames.Canvas.CubicBeziers).Inputs;
        Assert.Equal(6f, curve.DashLength);
        Assert.Equal(4f, curve.GapLength);
    }

    [Fact]
    public void A_dashed_line_is_one_line_with_its_dashes_and_gaps()
    {
        var frames = Painted(p => p.DashedLine(new PointF(10f, 10f), new PointF(90f, 10f), 1.5f, Ink, 5f, 3f));

        var line = Assert.Single(frames.Canvas.Lines).Inputs;
        Assert.Equal(OnCanvas(10f, 10f), line.Start);
        Assert.Equal(OnCanvas(90f, 10f), line.End);
        Assert.Equal(5f, line.DashLength);
        Assert.Equal(3f, line.GapLength);
        Assert.Equal(1.5f, line.Thickness);
    }

    [Fact]
    public void A_hollow_arrowhead_is_a_closed_triangle_of_three_lines_pointing_at_its_tip()
    {
        var frames = Painted(p => p.HollowArrowhead(new PointF(100f, 50f), new PointF(0f, 50f), 12f, 10f, 1f, Ink));

        var lines = frames.Canvas.Lines
            .Select(static l => (l.Inputs.Start, l.Inputs.End))
            .ToList();
        Assert.Equal(
        [
            (OnCanvas(100f, 50f), OnCanvas(88f, 45f)),
            (OnCanvas(88f, 45f), OnCanvas(88f, 55f)),
            (OnCanvas(88f, 55f), OnCanvas(100f, 50f)),
        ], lines);
    }

    [Fact]
    public void An_open_arrowhead_is_two_strokes_meeting_at_its_tip()
    {
        var frames = Painted(p => p.OpenArrowhead(new PointF(50f, 100f), new PointF(50f, 0f), 10f, 8f, 1f, Ink));

        var lines = frames.Canvas.Lines
            .Select(static l => (l.Inputs.Start, l.Inputs.End))
            .ToList();
        Assert.Equal(
        [
            (OnCanvas(54f, 90f), OnCanvas(50f, 100f)),
            (OnCanvas(46f, 90f), OnCanvas(50f, 100f)),
        ], lines);
    }

    [Fact]
    public void An_arrowhead_whose_tail_is_its_tip_points_down()
    {
        var frames = Painted(p => p.OpenArrowhead(new PointF(50f, 50f), new PointF(50f, 50f), 10f, 8f, 1f, Ink));

        var ends = frames.Canvas.Lines
            .Select(static l => l.Inputs.Start)
            .ToList();
        Assert.All(ends, e => Assert.Equal(OnCanvas(0f, 40f).Y, e.Y));
    }
}
