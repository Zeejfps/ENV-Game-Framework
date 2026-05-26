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

        // Channel deltas must be computed in signed arithmetic. When an 'over' channel is
        // darker than the 'under' channel (common in Light themes where surfaces are near-
        // white), (ar - br) in unsigned uint wraps to ~0xFFFFFFxx and the float multiply +
        // (uint) cast produces a huge value that bleeds into the wrong channel slot.
        int ar = (int)((over >> 16) & 0xFF);
        int ag = (int)((over >> 8) & 0xFF);
        int ab = (int)(over & 0xFF);

        int br = (int)((under >> 16) & 0xFF);
        int bg = (int)((under >> 8) & 0xFF);
        int bb = (int)(under & 0xFF);
        uint ba = (under >> 24) & 0xFF;

        uint r = (uint)Math.Clamp((int)(br + (ar - br) * alpha + 0.5f), 0, 255);
        uint g = (uint)Math.Clamp((int)(bg + (ag - bg) * alpha + 0.5f), 0, 255);
        uint b = (uint)Math.Clamp((int)(bb + (ab - bb) * alpha + 0.5f), 0, 255);

        return (ba << 24) | (r << 16) | (g << 8) | b;
    }

    private static uint WithAlphaFrom(uint color, uint alphaSource) =>
        (color & 0x00FFFFFF) | (alphaSource & 0xFF000000);
}
