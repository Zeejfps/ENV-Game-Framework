namespace ZGF.Gui.Immediate;

/// <summary>
/// Animates a change over a fixed <see cref="Duration"/> along an easing curve, after an optional
/// <see cref="Delay"/>. A target that changes mid-flight restarts the tween from wherever the value
/// is, the way CSS transitions do.
/// </summary>
public readonly struct Tween
{
    private readonly Func<float, float>? _curve;

    public Tween(float duration, Func<float, float> curve, float delay = 0f)
    {
        Duration = MathF.Max(0f, duration);
        Delay = MathF.Max(0f, delay);
        _curve = curve;
    }

    public float Duration { get; }
    public float Delay { get; }

    public float Evaluate(float progress) => _curve is { } curve ? curve(progress) : progress;

    public static Tween Linear(float duration, float delay = 0f) => new(duration, Ease.Linear, delay);
    public static Tween InQuad(float duration, float delay = 0f) => new(duration, Ease.InQuad, delay);
    public static Tween OutQuad(float duration, float delay = 0f) => new(duration, Ease.OutQuad, delay);
    public static Tween InOutQuad(float duration, float delay = 0f) => new(duration, Ease.InOutQuad, delay);
    public static Tween InCubic(float duration, float delay = 0f) => new(duration, Ease.InCubic, delay);
    public static Tween OutCubic(float duration, float delay = 0f) => new(duration, Ease.OutCubic, delay);
    public static Tween InOutCubic(float duration, float delay = 0f) => new(duration, Ease.InOutCubic, delay);
    public static Tween InQuart(float duration, float delay = 0f) => new(duration, Ease.InQuart, delay);
    public static Tween OutQuart(float duration, float delay = 0f) => new(duration, Ease.OutQuart, delay);
    public static Tween InOutQuart(float duration, float delay = 0f) => new(duration, Ease.InOutQuart, delay);
    public static Tween InSine(float duration, float delay = 0f) => new(duration, Ease.InSine, delay);
    public static Tween OutSine(float duration, float delay = 0f) => new(duration, Ease.OutSine, delay);
    public static Tween InOutSine(float duration, float delay = 0f) => new(duration, Ease.InOutSine, delay);
    public static Tween InExpo(float duration, float delay = 0f) => new(duration, Ease.InExpo, delay);
    public static Tween OutExpo(float duration, float delay = 0f) => new(duration, Ease.OutExpo, delay);
    public static Tween InOutExpo(float duration, float delay = 0f) => new(duration, Ease.InOutExpo, delay);
    public static Tween InBack(float duration, float delay = 0f) => new(duration, Ease.InBack, delay);
    public static Tween OutBack(float duration, float delay = 0f) => new(duration, Ease.OutBack, delay);
    public static Tween InOutBack(float duration, float delay = 0f) => new(duration, Ease.InOutBack, delay);
    public static Tween InElastic(float duration, float delay = 0f) => new(duration, Ease.InElastic, delay);
    public static Tween OutElastic(float duration, float delay = 0f) => new(duration, Ease.OutElastic, delay);
    public static Tween InBounce(float duration, float delay = 0f) => new(duration, Ease.InBounce, delay);
    public static Tween OutBounce(float duration, float delay = 0f) => new(duration, Ease.OutBounce, delay);
    public static Tween InOutBounce(float duration, float delay = 0f) => new(duration, Ease.InOutBounce, delay);

    /// <summary>A CSS-style cubic Bézier curve. Create the <see cref="CubicBezierEasing"/> once (a
    /// static field) and reuse it.</summary>
    public static Tween CubicBezier(float duration, CubicBezierEasing curve, float delay = 0f) =>
        new(duration, curve.Function, delay);

    /// <summary>Any curve from 0..1 to 0..1. A non-capturing lambda costs nothing per call.</summary>
    public static Tween Custom(float duration, Func<float, float> curve, float delay = 0f) =>
        new(duration, curve, delay);
}
