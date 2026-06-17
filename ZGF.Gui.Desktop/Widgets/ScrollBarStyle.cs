namespace ZGF.Gui.Desktop.Widgets;

/// <summary>
/// Colors for a <see cref="ScrollBar"/> and the <c>ScrollArea</c> that hosts one: the track's
/// fill/border and the thumb's idle/hover fill and border. Authored as a single value so a host
/// themes every scrollbar from one place; <see cref="Default"/> is the built-in grey look used
/// when no style is supplied.
/// </summary>
public readonly record struct ScrollBarStyle
{
    public uint TrackBackground { get; init; }
    public BorderSizeStyle TrackBorderSize { get; init; }
    public BorderColorStyle TrackBorder { get; init; }
    public uint ThumbIdleBackground { get; init; }
    public uint ThumbHoverBackground { get; init; }
    public BorderSizeStyle ThumbBorderSize { get; init; }
    public BorderColorStyle ThumbBorder { get; init; }

    public static ScrollBarStyle Default { get; } = new()
    {
        TrackBackground = 0xFFCECECE,
        TrackBorderSize = BorderSizeStyle.All(1),
        TrackBorder = Bevel,
        ThumbIdleBackground = 0xFFCECECE,
        ThumbHoverBackground = 0xFFE2E2E2,
        ThumbBorderSize = BorderSizeStyle.All(1),
        ThumbBorder = Bevel,
    };

    private static BorderColorStyle Bevel => new()
    {
        Left = 0xFF9C9C9C,
        Top = 0xFF9C9C9C,
        Right = 0xFFFFFFFF,
        Bottom = 0xFFFFFFFF,
    };
}
