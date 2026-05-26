namespace ZGF.Gui;

public static class ColorMath
{
    /// <summary>
    /// Composite <paramref name="over"/> on top of <paramref name="under"/> at the given
    /// <paramref name="alpha"/> in [0,1]. Both colors are ARGB (alpha-high byte). The
    /// result's alpha channel is taken from <paramref name="under"/> — this is a paint-on-
    /// surface blend, not a porter-duff Over.
    /// </summary>
    public static uint Blend(uint over, uint under, float alpha)
    {
        if (alpha <= 0f) return under;
        if (alpha >= 1f) return WithAlphaFrom(over, under);

        var ar = (over >> 16) & 0xFF;
        var ag = (over >> 8) & 0xFF;
        var ab = over & 0xFF;

        var br = (under >> 16) & 0xFF;
        var bg = (under >> 8) & 0xFF;
        var bb = under & 0xFF;
        var ba = (under >> 24) & 0xFF;

        var r = (uint)(br + (ar - br) * alpha);
        var g = (uint)(bg + (ag - bg) * alpha);
        var b = (uint)(bb + (ab - bb) * alpha);

        return (ba << 24) | (r << 16) | (g << 8) | b;
    }

    private static uint WithAlphaFrom(uint color, uint alphaSource) =>
        (color & 0x00FFFFFF) | (alphaSource & 0xFF000000);
}
