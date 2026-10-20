namespace ZGF.Gui.Immediate;

/// <summary>Easing curves: each maps progress 0..1 to eased progress, 0 at 0 and 1 at 1. Back,
/// elastic and bounce curves overshoot the range in between.</summary>
public static class Ease
{
    private const float BackOvershoot = 1.70158f;
    private const float BackInOutOvershoot = BackOvershoot * 1.525f;
    private const float ElasticPeriod = 2f * MathF.PI / 3f;

    public static float Linear(float t) => t;

    public static float InQuad(float t) => t * t;
    public static float OutQuad(float t) => 1f - (1f - t) * (1f - t);
    public static float InOutQuad(float t) => t < 0.5f ? 2f * t * t : 1f - Pow(-2f * t + 2f, 2) / 2f;

    public static float InCubic(float t) => t * t * t;
    public static float OutCubic(float t) => 1f - Pow(1f - t, 3);
    public static float InOutCubic(float t) => t < 0.5f ? 4f * t * t * t : 1f - Pow(-2f * t + 2f, 3) / 2f;

    public static float InQuart(float t) => t * t * t * t;
    public static float OutQuart(float t) => 1f - Pow(1f - t, 4);
    public static float InOutQuart(float t) => t < 0.5f ? 8f * t * t * t * t : 1f - Pow(-2f * t + 2f, 4) / 2f;

    public static float InSine(float t) => 1f - MathF.Cos(t * MathF.PI / 2f);
    public static float OutSine(float t) => MathF.Sin(t * MathF.PI / 2f);
    public static float InOutSine(float t) => -(MathF.Cos(MathF.PI * t) - 1f) / 2f;

    public static float InExpo(float t) => t <= 0f ? 0f : MathF.Pow(2f, 10f * t - 10f);
    public static float OutExpo(float t) => t >= 1f ? 1f : 1f - MathF.Pow(2f, -10f * t);
    public static float InOutExpo(float t) => t <= 0f ? 0f
        : t >= 1f ? 1f
        : t < 0.5f ? MathF.Pow(2f, 20f * t - 10f) / 2f
        : (2f - MathF.Pow(2f, -20f * t + 10f)) / 2f;

    public static float InBack(float t) => (BackOvershoot + 1f) * t * t * t - BackOvershoot * t * t;
    public static float OutBack(float t) => 1f + (BackOvershoot + 1f) * Pow(t - 1f, 3) + BackOvershoot * Pow(t - 1f, 2);
    public static float InOutBack(float t) => t < 0.5f
        ? Pow(2f * t, 2) * ((BackInOutOvershoot + 1f) * 2f * t - BackInOutOvershoot) / 2f
        : (Pow(2f * t - 2f, 2) * ((BackInOutOvershoot + 1f) * (2f * t - 2f) + BackInOutOvershoot) + 2f) / 2f;

    public static float InElastic(float t) => t <= 0f ? 0f
        : t >= 1f ? 1f
        : -MathF.Pow(2f, 10f * t - 10f) * MathF.Sin((10f * t - 10.75f) * ElasticPeriod);
    public static float OutElastic(float t) => t <= 0f ? 0f
        : t >= 1f ? 1f
        : MathF.Pow(2f, -10f * t) * MathF.Sin((10f * t - 0.75f) * ElasticPeriod) + 1f;

    public static float InBounce(float t) => 1f - OutBounce(1f - t);
    public static float OutBounce(float t)
    {
        const float n = 7.5625f;
        const float d = 2.75f;
        if (t < 1f / d) return n * t * t;
        if (t < 2f / d) return n * (t -= 1.5f / d) * t + 0.75f;
        if (t < 2.5f / d) return n * (t -= 2.25f / d) * t + 0.9375f;
        return n * (t -= 2.625f / d) * t + 0.984375f;
    }
    public static float InOutBounce(float t) => t < 0.5f
        ? (1f - OutBounce(1f - 2f * t)) / 2f
        : (1f + OutBounce(2f * t - 1f)) / 2f;

    /// <summary>The CSS <c>cubic-bezier(x1, y1, x2, y2)</c> timing function. It allocates, so keep
    /// one in a static field rather than creating it per frame.</summary>
    public static CubicBezierEasing CubicBezier(float x1, float y1, float x2, float y2) => new(x1, y1, x2, y2);

    private static float Pow(float value, int exponent)
    {
        var result = 1f;
        for (var i = 0; i < exponent; i++)
            result *= value;
        return result;
    }
}

/// <summary>
/// A CSS <c>cubic-bezier()</c> timing curve from (0,0) to (1,1) through two control points. The x
/// controls must lie in 0..1 so the curve is a function of time; the y controls may leave it, for
/// overshoot.
/// </summary>
public sealed class CubicBezierEasing
{
    private const float Epsilon = 1e-6f;
    private const int NewtonIterations = 8;
    private const int BisectionIterations = 32;

    private readonly float _ax, _bx, _cx;
    private readonly float _ay, _by, _cy;

    public CubicBezierEasing(float x1, float y1, float x2, float y2)
    {
        if (x1 is < 0f or > 1f || x2 is < 0f or > 1f)
            throw new ArgumentOutOfRangeException(nameof(x1), "Cubic Bézier easing x controls must lie in 0..1.");

        _cx = 3f * x1;
        _bx = 3f * (x2 - x1) - _cx;
        _ax = 1f - _cx - _bx;
        _cy = 3f * y1;
        _by = 3f * (y2 - y1) - _cy;
        _ay = 1f - _cy - _by;
        Function = Evaluate;
    }

    /// <summary>This curve as a delegate, created once, for <see cref="Tween.Custom"/>.</summary>
    public Func<float, float> Function { get; }

    public float Evaluate(float t)
    {
        if (t <= 0f) return 0f;
        if (t >= 1f) return 1f;
        var u = SolveForParameter(t);
        return ((_ay * u + _by) * u + _cy) * u;
    }

    // Finds the curve parameter whose x is the given time: Newton's method converges in a few steps
    // where the slope allows, and bisection takes over where it doesn't.
    private float SolveForParameter(float x)
    {
        var u = x;
        for (var i = 0; i < NewtonIterations; i++)
        {
            var error = SampleX(u) - x;
            if (MathF.Abs(error) < Epsilon) return u;
            var slope = (3f * _ax * u + 2f * _bx) * u + _cx;
            if (MathF.Abs(slope) < Epsilon) break;
            u -= error / slope;
        }

        var low = 0f;
        var high = 1f;
        u = x;
        for (var i = 0; i < BisectionIterations; i++)
        {
            var sample = SampleX(u);
            if (MathF.Abs(sample - x) < Epsilon) return u;
            if (x > sample) low = u;
            else high = u;
            u = (low + high) / 2f;
        }
        return u;
    }

    private float SampleX(float u) => ((_ax * u + _bx) * u + _cx) * u;
}
