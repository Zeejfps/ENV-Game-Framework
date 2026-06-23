namespace ZGF.Gui;

/// <summary>Where a shaped line sits within its box on the horizontal axis.</summary>
public enum TextPlacement
{
    Left,
    Center,
    Right,
}

/// <summary>Resolves a writing-direction-relative <see cref="TextAlignment"/> to a concrete edge.</summary>
public static class TextLayout
{
    /// <summary>
    /// Maps a <see cref="TextAlignment"/> to a physical <see cref="TextPlacement"/> for the given base
    /// direction: <see cref="TextAlignment.Start"/> is the leading edge (left under LTR, right under RTL)
    /// and <see cref="TextAlignment.End"/> the trailing edge, so the same widget right-aligns its labels
    /// when the UI flips to a right-to-left locale.
    /// </summary>
    public static TextPlacement ResolveHorizontal(TextAlignment alignment, bool rtlBase) => alignment switch
    {
        TextAlignment.Center => TextPlacement.Center,
        TextAlignment.End => rtlBase ? TextPlacement.Left : TextPlacement.Right,
        _ => rtlBase ? TextPlacement.Right : TextPlacement.Left,
    };
}
