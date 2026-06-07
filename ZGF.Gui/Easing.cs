namespace ZGF.Gui;

/// <summary>Normalized easing curves and interpolation: the input progress and the returned value
/// are both in [0, 1]. Shared by animated views so transitions feel consistent.</summary>
public static class Easing
{
    /// <summary>Cubic ease-out — fast start, decelerating to a gentle stop. The standard curve for
    /// elements entering/settling (close to the system keyboard's slide feel).</summary>
    public static float OutCubic(float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        var inv = 1f - t;
        return 1f - inv * inv * inv;
    }

    /// <summary>Cubic ease-in-out — slow start, fast middle, slow end.</summary>
    public static float InOutCubic(float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return t < 0.5f
            ? 4f * t * t * t
            : 1f - MathF.Pow(-2f * t + 2f, 3f) / 2f;
    }

    public static float Lerp(float from, float to, float t) => from + (to - from) * t;
}
