using System.Globalization;

namespace ZGF.Gui.Immediate;

/// <summary>Helpers for 0xAARRGGBB colors.</summary>
public static class UiColor
{
    private const int OpaqueHexLength = 7;
    private const int TranslucentHexLength = 9;
    private const uint OpaqueAlpha = 0xFF000000;

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

    /// <summary>Reads <paramref name="text"/> written as CSS writes a color, <c>#RRGGBB</c> or
    /// <c>#RRGGBBAA</c>, as 0xAARRGGBB; six digits are opaque.</summary>
    public static bool TryParse(ReadOnlySpan<char> text, out uint color)
    {
        color = 0;
        if (text.Length != OpaqueHexLength && text.Length != TranslucentHexLength)
            return false;
        if (text[0] != '#')
            return false;
        var hex = text.Slice(1);
        if (!uint.TryParse(hex, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var digits))
            return false;
        color = ToArgb(digits, text.Length == TranslucentHexLength);
        return true;
    }

    private static uint ToArgb(uint digits, bool hasAlpha)
    {
        if (!hasAlpha)
            return OpaqueAlpha | digits;
        var alpha = digits & 0xFFu;
        var rgb = digits >> 8;
        return (alpha << 24) | rgb;
    }
}
