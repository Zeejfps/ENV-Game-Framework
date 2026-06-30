using System.Numerics;
using ZGF.Geometry;

namespace ZGF.Gui;

/// <summary>Evaluates cubic Béziers and flattens them into the quadratic segments the canvas can
/// stroke natively. The shape shader has only a quadratic SDF, so a cubic (two control points, able
/// to form an S with an inflection) is approximated by adaptively subdividing it — de Casteljau at
/// the midpoint until each piece is within tolerance of a single quadratic — and emitting one
/// <see cref="ICanvas.DrawBezier"/> per piece. Colour gradient and dashing ride along on each
/// piece; the gradient is split across pieces by curve parameter, dashing restarts its phase per
/// piece (negligible for thin strokes, the only place it's used).</summary>
public static class CubicBezier
{
    public const float DefaultFlatness = 0.1f;

    // sqrt(3)/36: the bound on how far a cubic piece deviates from its best single quadratic, per
    // unit of the piece's third-difference vector |p0 - 3p1 + 3p2 - p3|. Each midpoint split divides
    // that vector by 8, so subdivision converges in a handful of levels.
    private const float ErrorPerThirdDifference = 0.0481125225f;
    private const int MaxDepth = 10;

    public static PointF Evaluate(PointF p0, PointF p1, PointF p2, PointF p3, float t)
    {
        var u = 1f - t;
        var uu = u * u;
        var tt = t * t;
        var w0 = uu * u;
        var w1 = 3f * uu * t;
        var w2 = 3f * u * tt;
        var w3 = tt * t;
        return new PointF(
            w0 * p0.X + w1 * p1.X + w2 * p2.X + w3 * p3.X,
            w0 * p0.Y + w1 * p1.Y + w2 * p2.Y + w3 * p3.Y);
    }

    /// <summary>Flattens the cubic and strokes each resulting quadratic piece onto <paramref name="canvas"/>.
    /// <paramref name="flatness"/> is the max allowed deviation, in the canvas's local coordinate units.</summary>
    public static void Flatten(in DrawCubicBezierInputs inputs, ICanvas canvas, float flatness = DefaultFlatness)
    {
        var sink = new CanvasSink(canvas);
        FlattenCore(in inputs, flatness, ref sink);
    }

    /// <summary>Flattens the cubic into quadratic pieces, appending each as a <see cref="DrawBezierInputs"/>
    /// to <paramref name="output"/>. Exposed for tests and callers that want the pieces directly.</summary>
    public static void Flatten(in DrawCubicBezierInputs inputs, List<DrawBezierInputs> output, float flatness = DefaultFlatness)
    {
        var sink = new ListSink(output);
        FlattenCore(in inputs, flatness, ref sink);
    }

    private static void FlattenCore<TSink>(in DrawCubicBezierInputs inputs, float flatness, ref TSink sink)
        where TSink : struct, IQuadSink
    {
        var p0 = new Vector2(inputs.Start.X, inputs.Start.Y);
        var p1 = new Vector2(inputs.Control1.X, inputs.Control1.Y);
        var p2 = new Vector2(inputs.Control2.X, inputs.Control2.Y);
        var p3 = new Vector2(inputs.End.X, inputs.End.Y);
        var tol = MathF.Max(flatness, 1e-4f);
        Subdivide(in inputs, p0, p1, p2, p3, 0f, 1f, tol, 0, ref sink);
    }

    private static void Subdivide<TSink>(
        in DrawCubicBezierInputs inputs,
        Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3,
        float t0, float t1, float tol, int depth, ref TSink sink)
        where TSink : struct, IQuadSink
    {
        var thirdDiff = p0 - 3f * p1 + 3f * p2 - p3;
        var error = ErrorPerThirdDifference * thirdDiff.Length();
        if (depth >= MaxDepth || error <= tol)
        {
            EmitQuad(in inputs, p0, p1, p2, p3, t0, t1, ref sink);
            return;
        }

        var a = (p0 + p1) * 0.5f;
        var b = (p1 + p2) * 0.5f;
        var c = (p2 + p3) * 0.5f;
        var ab = (a + b) * 0.5f;
        var bc = (b + c) * 0.5f;
        var mid = (ab + bc) * 0.5f;
        var tm = (t0 + t1) * 0.5f;
        Subdivide(in inputs, p0, a, ab, mid, t0, tm, tol, depth + 1, ref sink);
        Subdivide(in inputs, mid, bc, c, p3, tm, t1, tol, depth + 1, ref sink);
    }

    private static void EmitQuad<TSink>(
        in DrawCubicBezierInputs inputs,
        Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3,
        float t0, float t1, ref TSink sink)
        where TSink : struct, IQuadSink
    {
        // Best single-quadratic control point for this cubic piece (average of the two
        // endpoint-tangent estimates).
        var control = (3f * p1 + 3f * p2 - p0 - p3) * 0.25f;

        var gradient = inputs.GradientEndColor.HasValue;
        var startColor = gradient ? LerpColor(inputs.Color, inputs.GradientEndColor!.Value, t0) : inputs.Color;
        var endColor = gradient ? LerpColor(inputs.Color, inputs.GradientEndColor!.Value, t1) : inputs.Color;

        sink.Emit(new DrawBezierInputs
        {
            Start = new PointF(p0.X, p0.Y),
            Control = new PointF(control.X, control.Y),
            End = new PointF(p3.X, p3.Y),
            Thickness = inputs.Thickness,
            Color = startColor,
            ZIndex = inputs.ZIndex,
            GradientEndColor = gradient ? endColor : null,
            DashLength = inputs.DashLength,
            GapLength = inputs.GapLength,
        });
    }

    private static uint LerpColor(uint a, uint b, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        var inv = 1f - t;
        var ca = (uint)(((a >> 24) & 0xFFu) * inv + ((b >> 24) & 0xFFu) * t + 0.5f);
        var cr = (uint)(((a >> 16) & 0xFFu) * inv + ((b >> 16) & 0xFFu) * t + 0.5f);
        var cg = (uint)(((a >> 8) & 0xFFu) * inv + ((b >> 8) & 0xFFu) * t + 0.5f);
        var cb = (uint)((a & 0xFFu) * inv + (b & 0xFFu) * t + 0.5f);
        return (ca << 24) | (cr << 16) | (cg << 8) | cb;
    }

    // A generic struct sink keeps the canvas path allocation-free while letting tests collect pieces.
    private interface IQuadSink
    {
        void Emit(in DrawBezierInputs quad);
    }

    private readonly struct CanvasSink(ICanvas canvas) : IQuadSink
    {
        public void Emit(in DrawBezierInputs quad) => canvas.DrawBezier(quad);
    }

    private readonly struct ListSink(List<DrawBezierInputs> list) : IQuadSink
    {
        public void Emit(in DrawBezierInputs quad) => list.Add(quad);
    }
}
