namespace ZGF.Desktop.Backends;

/// <summary>The pixels of a cursor shape ZGF draws itself, where the platform has none: RGBA, top row first,
/// <see cref="Size"/> pixels square, with the pointer's hotspot at (<see cref="HotspotX"/>, <see cref="HotspotY"/>).</summary>
internal readonly record struct CursorImage(int Size, byte[] Rgba, int HotspotX, int HotspotY);

/// <summary>The open and the closed hand, drawn as the classic 16-pixel hands are: a black outline around white, so
/// they read on light and dark backgrounds alike.</summary>
internal static class DrawnCursors
{
    private const int ArtSize = 16;

    // X is the outline, o the fill, . transparent.
    private static readonly string[] OpenHand =
    [
        ".......XX.......",
        "...XX.XooXXX....",
        "..XooXXooXooX...",
        "..XooXXooXooX.X.",
        "...XooXooXooXXoX",
        "...XooXooXooXooX",
        ".XX.XooooooXooX.",
        "XooXXoooooooooX.",
        "XoooXooooooooX..",
        ".XooooooooooooX.",
        "..XoooooooooooX.",
        "..XooooooooooX..",
        "...XoooooooooX..",
        "....XoooooooX...",
        ".....XooooooX...",
        ".....XooooooX...",
    ];

    private static readonly string[] ClosedHand =
    [
        "................",
        "................",
        "................",
        "................",
        "....XX.XX.XX....",
        "...XooXooXooXX..",
        "...XoooooooXooX.",
        "..XXoooooooooX..",
        ".XooXooooooooX..",
        ".XoooooooooooX..",
        "..XooooooooooX..",
        "..XoooooooooX...",
        "...XooooooooX...",
        "....XoooooooX...",
        "....XoooooooX...",
        "................",
    ];

    /// <summary>Whether ZGF draws <paramref name="cursor"/> rather than asking the platform for it.</summary>
    public static bool Draws(MouseCursor cursor) => cursor is MouseCursor.Grab or MouseCursor.Grabbing;

    /// <summary>The pixels of <paramref name="cursor"/>, one of the shapes <see cref="Draws"/> says ZGF draws, each art
    /// pixel <paramref name="scale"/> pixels square, as a screen at that content scale shows the platform's own
    /// cursors.</summary>
    public static CursorImage Draw(MouseCursor cursor, int scale)
    {
        var art = cursor switch
        {
            MouseCursor.Grab => OpenHand,
            MouseCursor.Grabbing => ClosedHand,
            _ => throw new ArgumentOutOfRangeException(nameof(cursor), cursor, "ZGF draws only the hands."),
        };
        if (scale < 1)
            throw new ArgumentOutOfRangeException(nameof(scale), scale, "A cursor is at least one pixel per art pixel.");

        var size = ArtSize * scale;
        var rgba = new byte[size * size * 4];
        for (var y = 0; y < size; y++)
        {
            var row = art[y / scale];
            for (var x = 0; x < size; x++)
            {
                var gray = row[x / scale] switch
                {
                    'X' => (byte)0,
                    'o' => (byte)255,
                    _ => (byte?)null,
                };
                if (gray == null)
                    continue;
                var at = (y * size + x) * 4;
                rgba[at] = rgba[at + 1] = rgba[at + 2] = gray.Value;
                rgba[at + 3] = 255;
            }
        }
        var hotspot = ArtSize / 2 * scale;
        return new CursorImage(size, rgba, hotspot, hotspot);
    }
}
