using ZGF.Gui.Immediate;
using ZGF.ILSpy.Core;

namespace ZGF.ILSpy;

/// <summary>Syntax colors for the code view, one per <see cref="SpanKind"/>, close to Visual
/// Studio's defaults.</summary>
public sealed class CodeViewPalette
{
    private readonly uint[] _kinds;

    private CodeViewPalette(uint[] kinds, uint error, uint match, uint currentMatch)
    {
        _kinds = kinds;
        Error = error;
        Match = match;
        CurrentMatch = currentMatch;
    }

    public uint Error { get; }
    public uint Match { get; }
    public uint CurrentMatch { get; }

    public uint this[SpanKind kind] => _kinds[(int)kind];

    public static CodeViewPalette Dark { get; } = new(
    [
        0xFFDCDCDC, 0xFF569CD6, 0xFF4EC9B0, 0xFFC8C8C8, 0xFFDCDCAA, 0xFFDCDCDC, 0xFFDCDCDC, 0xFFDCDCDC,
        0xFF9CDCFE, 0xFF9CDCFE, 0xFFD69D85, 0xFFB5CEA8, 0xFF57A64A, 0xFF9B9B9B, 0xFF569CD6, 0xFFB8D7A3,
    ], 0xFFF48771, 0xFF5C4426, 0xFF9E6A1E);

    public static CodeViewPalette Light { get; } = new(
    [
        0xFF000000, 0xFF0000FF, 0xFF2B91AF, 0xFF000000, 0xFF74531F, 0xFF000000, 0xFF000000, 0xFF000000,
        0xFF1F377F, 0xFF1F377F, 0xFFA31515, 0xFF098658, 0xFF008000, 0xFF808080, 0xFF0000FF, 0xFF1F377F,
    ], 0xFFC72E2E, 0xFFF8E2B8, 0xFFF2B866);

    /// <summary><see cref="Dark"/> on a dark background, else <see cref="Light"/>.</summary>
    public static CodeViewPalette For(UiTheme theme) => IsDark(theme.Background) ? Dark : Light;

    private static bool IsDark(uint color)
    {
        var r = (color >> 16) & 0xFF;
        var g = (color >> 8) & 0xFF;
        var b = color & 0xFF;
        return 0.2126f * r + 0.7152f * g + 0.0722f * b < 128f;
    }
}
