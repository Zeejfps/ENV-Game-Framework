using System;
using System.Collections.Generic;
using System.Linq;
using ZGF.Geometry;
using ZGF.Gui.Testing;

namespace ZGF.Gui.Tests;

/// <summary>The cubic bezier primitive has no SDF in the shape shader; it is flattened into
/// quadratic pieces that the canvas strokes natively. These cover the flattening math (continuity,
/// accuracy, adaptivity) and that colour/dash/z ride along onto each piece.</summary>
public class CubicBezierTests
{
    private static DrawCubicBezierInputs Cubic(
        PointF p0, PointF p1, PointF p2, PointF p3,
        uint color = 0xFF112233u, uint? gradientEnd = null, float dash = 0f, float gap = 0f, int z = 4)
        => new()
        {
            Start = p0, Control1 = p1, Control2 = p2, End = p3,
            Thickness = 2f, Color = color, ZIndex = z,
            GradientEndColor = gradientEnd, DashLength = dash, GapLength = gap,
        };

    private static List<DrawBezierInputs> Flatten(DrawCubicBezierInputs c, float flatness = CubicBezier.DefaultFlatness)
    {
        var output = new List<DrawBezierInputs>();
        CubicBezier.Flatten(in c, output, flatness);
        return output;
    }

    [Fact]
    public void Evaluate_MatchesCubicFormula()
    {
        var p0 = new PointF(0, 0);
        var p1 = new PointF(0, 10);
        var p2 = new PointF(10, 10);
        var p3 = new PointF(10, 0);
        foreach (var t in new[] { 0f, 0.25f, 0.5f, 0.75f, 1f })
        {
            var u = 1 - t;
            var expX = u * u * u * p0.X + 3 * u * u * t * p1.X + 3 * u * t * t * p2.X + t * t * t * p3.X;
            var expY = u * u * u * p0.Y + 3 * u * u * t * p1.Y + 3 * u * t * t * p2.Y + t * t * t * p3.Y;
            var got = CubicBezier.Evaluate(p0, p1, p2, p3, t);
            Assert.Equal(expX, got.X, 4);
            Assert.Equal(expY, got.Y, 4);
        }
    }

    [Fact]
    public void Flatten_StraightLine_ProducesSingleSegment()
    {
        // Controls collinear with the endpoints: a cubic with zero third-difference is exactly one quadratic.
        var c = Cubic(new PointF(0, 0), new PointF(10, 0), new PointF(20, 0), new PointF(30, 0));
        var segs = Flatten(c);
        Assert.Single(segs);
        Assert.Equal(0f, segs[0].Start.X, 4);
        Assert.Equal(30f, segs[0].End.X, 4);
    }

    [Fact]
    public void Flatten_SCurve_Subdivides_AndStaysContinuous()
    {
        // A genuine S needs an inflection a single quadratic can't represent, so it must subdivide.
        var c = Cubic(new PointF(0, 0), new PointF(0, 20), new PointF(20, 0), new PointF(20, 20));
        var segs = Flatten(c);

        Assert.True(segs.Count >= 2, $"expected subdivision, got {segs.Count} segment(s)");

        // Endpoints preserved, pieces chain end-to-start.
        Assert.True(Close(segs[0].Start, c.Start));
        Assert.True(Close(segs[^1].End, c.End));
        for (var i = 0; i < segs.Count - 1; i++)
            Assert.True(Close(segs[i].End, segs[i + 1].Start), $"discontinuity at join {i}");
    }

    [Fact]
    public void Flatten_StaysWithinTolerance_OfTheTrueCubic()
    {
        var c = Cubic(new PointF(0, 0), new PointF(10, 40), new PointF(30, -20), new PointF(40, 20));
        const float flatness = 0.1f;
        var segs = Flatten(c, flatness);

        // Densely sample the true cubic; every point must be near the flattened quad polyline.
        var maxDist = 0f;
        for (var i = 0; i <= 200; i++)
        {
            var pt = CubicBezier.Evaluate(c.Start, c.Control1, c.Control2, c.End, i / 200f);
            var d = MinDistanceToSegments(pt, segs);
            maxDist = MathF.Max(maxDist, d);
        }
        Assert.True(maxDist < 0.25f, $"max deviation {maxDist} exceeded tolerance");
    }

    [Fact]
    public void Flatten_TighterTolerance_ProducesMoreSegments()
    {
        var c = Cubic(new PointF(0, 0), new PointF(0, 20), new PointF(20, 0), new PointF(20, 20));
        Assert.True(Flatten(c, 0.01f).Count > Flatten(c, 1f).Count);
    }

    [Fact]
    public void Flatten_Solid_AllSegmentsCarrySolidColor()
    {
        var c = Cubic(new PointF(0, 0), new PointF(0, 20), new PointF(20, 0), new PointF(20, 20), color: 0xFFABCDEFu);
        foreach (var seg in Flatten(c))
        {
            Assert.Equal(0xFFABCDEFu, seg.Color);
            Assert.Null(seg.GradientEndColor);
        }
    }

    [Fact]
    public void Flatten_Gradient_PreservesEndpointsAndIsContinuousAcrossJoins()
    {
        const uint start = 0xFF000000u;
        const uint end = 0xFFFFFFFFu;
        var c = Cubic(new PointF(0, 0), new PointF(0, 20), new PointF(20, 0), new PointF(20, 20), gradientEnd: end);
        c = c with { Color = start };
        var segs = Flatten(c);

        // Global gradient endpoints land exactly on the first/last pieces.
        Assert.Equal(start, segs[0].Color);
        Assert.Equal(end, segs[^1].GradientEndColor);

        // Each piece is a gradient, and the colour is continuous where pieces meet.
        for (var i = 0; i < segs.Count; i++)
        {
            Assert.NotNull(segs[i].GradientEndColor);
            if (i < segs.Count - 1)
                Assert.Equal(segs[i].GradientEndColor!.Value, segs[i + 1].Color);
        }
    }

    [Fact]
    public void Flatten_PropagatesThicknessZIndexAndDash()
    {
        var c = Cubic(new PointF(0, 0), new PointF(0, 20), new PointF(20, 0), new PointF(20, 20), dash: 4f, gap: 3f, z: 7);
        foreach (var seg in Flatten(c))
        {
            Assert.Equal(2f, seg.Thickness);
            Assert.Equal(7, seg.ZIndex);
            Assert.Equal(4f, seg.DashLength);
            Assert.Equal(3f, seg.GapLength);
        }
    }

    [Fact]
    public void Flatten_OntoCanvas_StrokesContinuousQuadraticPieces()
    {
        var capture = new QuadCaptureCanvas();
        var c = Cubic(new PointF(0, 0), new PointF(0, 20), new PointF(20, 0), new PointF(20, 20));
        CubicBezier.Flatten(in c, capture);

        Assert.True(capture.Quads.Count >= 2);
        Assert.True(Close(capture.Quads[0].Start, c.Start));
        Assert.True(Close(capture.Quads[^1].End, c.End));
    }

    [Fact]
    public void RecordingCanvas_RecordsTheRawCubic()
    {
        var canvas = new RecordingCanvas();
        var c = Cubic(new PointF(1, 2), new PointF(3, 4), new PointF(5, 6), new PointF(7, 8));
        canvas.DrawCubicBezier(in c);

        var recorded = Assert.Single(canvas.CubicBeziers);
        Assert.Equal(new PointF(1, 2), recorded.Inputs.Start);
        Assert.Equal(new PointF(7, 8), recorded.Inputs.End);
    }

    private static bool Close(PointF a, PointF b, float eps = 1e-3f)
        => MathF.Abs(a.X - b.X) <= eps && MathF.Abs(a.Y - b.Y) <= eps;

    private static float MinDistanceToSegments(PointF pt, List<DrawBezierInputs> segs)
    {
        var min = float.MaxValue;
        foreach (var seg in segs)
        {
            // Sample each quadratic piece into a short polyline and take the nearest segment.
            var prev = seg.Start;
            for (var i = 1; i <= 16; i++)
            {
                var cur = QuadEval(seg.Start, seg.Control, seg.End, i / 16f);
                min = MathF.Min(min, DistancePointToSegment(pt, prev, cur));
                prev = cur;
            }
        }
        return min;
    }

    private static PointF QuadEval(PointF a, PointF b, PointF c, float t)
    {
        var u = 1 - t;
        return new PointF(
            u * u * a.X + 2 * u * t * b.X + t * t * c.X,
            u * u * a.Y + 2 * u * t * b.Y + t * t * c.Y);
    }

    private static float DistancePointToSegment(PointF p, PointF a, PointF b)
    {
        float abx = b.X - a.X, aby = b.Y - a.Y;
        float apx = p.X - a.X, apy = p.Y - a.Y;
        var denom = abx * abx + aby * aby;
        var t = denom > 1e-9f ? Math.Clamp((apx * abx + apy * aby) / denom, 0f, 1f) : 0f;
        float cx = a.X + t * abx, cy = a.Y + t * aby;
        float dx = p.X - cx, dy = p.Y - cy;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>Minimal canvas that captures the quadratic pieces a cubic flattens into.</summary>
    private sealed class QuadCaptureCanvas : ICanvas
    {
        public List<DrawBezierInputs> Quads { get; } = new();
        public void DrawBezier(in DrawBezierInputs inputs) => Quads.Add(inputs);

        public void DrawRect(in DrawRectInputs inputs) { }
        public void DrawText(in DrawTextInputs inputs) { }
        public void DrawImage(in DrawImageInputs inputs) { }
        public void DrawBoxShadow(in DrawBoxShadowInputs inputs) { }
        public void DrawLine(in DrawLineInputs inputs) { }
        public void DrawCircle(in DrawCircleInputs inputs) { }
        public void DrawCubicBezier(in DrawCubicBezierInputs inputs) => CubicBezier.Flatten(in inputs, this);
        public bool TryGetClip(out RectF rect) { rect = default; return false; }
        public void PushClip(RectF rect) { }
        public void PopClip() { }
        public void PushOpacity(float opacity) { }
        public void PopOpacity() { }
        public void PushTranslation(float dx, float dy) { }
        public void PopTranslation() { }
        public void PushScale(float sx, float sy, float pivotX, float pivotY) { }
        public void PopScale() { }
        public float MeasureTextWidth(ReadOnlySpan<char> text, TextStyle style) => 0f;
        public float MeasureTextPrefix(ReadOnlySpan<char> text, int prefixLength, TextStyle style) => 0f;
        public float MeasureTextLineHeight(TextStyle style) => 0f;
        public int GetImageWidth(string imageId) => 0;
        public int GetImageHeight(string imageId) => 0;
    }
}
