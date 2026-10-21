namespace ZGF.Gui.Immediate;

/// <summary>Helpers for 0xAARRGGBB colors.</summary>
public static class UiColor
{
    /// <summary>Blends from <paramref name="a"/> toward <paramref name="b"/> by
    /// <paramref name="t"/>, channel by channel including alpha.</summary>
    public static uint Mix(uint a, uint b, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        var inv = 1f - t;
        var ca = (uint)(((a >> 24) & 0xFFu) * inv + ((b >> 24) & 0xFFu) * t + 0.5f);
        var cr = (uint)(((a >> 16) & 0xFFu) * inv + ((b >> 16) & 0xFFu) * t + 0.5f);
        var cg = (uint)(((a >> 8) & 0xFFu) * inv + ((b >> 8) & 0xFFu) * t + 0.5f);
        var cb = (uint)((a & 0xFFu) * inv + (b & 0xFFu) * t + 0.5f);
        return (ca << 24) | (cr << 16) | (cg << 8) | cb;
    }

    /// <summary>Relative luminance of <paramref name="color"/>'s RGB, 0 for black to 1 for white.</summary>
    public static float Luminance(uint color)
    {
        return 0.2126f * Linear((color >> 16) & 0xFFu) + 0.7152f * Linear((color >> 8) & 0xFFu) + 0.0722f * Linear(color & 0xFFu);

        static float Linear(uint channel)
        {
            var c = channel / 255f;
            return c <= 0.04045f ? c / 12.92f : MathF.Pow((c + 0.055f) / 1.055f, 2.4f);
        }
    }

    /// <summary>Whether text on <paramref name="color"/> should be light.</summary>
    public static bool IsDark(uint color) => Luminance(color) < 0.18f;
}
