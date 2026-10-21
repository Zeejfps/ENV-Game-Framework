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
}
