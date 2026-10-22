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
    public static Tween OutCubic(float duration, float delay = 0f) => new(duration, Ease.OutCubic, delay);

    /// <summary>Any curve from 0..1 to 0..1. A non-capturing lambda costs nothing per call.</summary>
    public static Tween Custom(float duration, Func<float, float> curve, float delay = 0f) =>
        new(duration, curve, delay);
}
