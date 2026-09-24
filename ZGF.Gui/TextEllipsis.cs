using System.Globalization;

namespace ZGF.Gui;

/// <summary>
/// Shortens text to a pixel budget with a trailing ellipsis, or a leading one for text whose end is
/// what matters (a path, whose file name is last). The cut lands on a cluster boundary:
/// a prefix that ends on half a surrogate pair renders as tofu, which reads as a missing glyph
/// rather than a truncation bug.
/// </summary>
public static class TextEllipsis
{
    private const string Ellipsis = "…";

    /// <summary>
    /// Returns <paramref name="text"/> shortened to fit in <paramref name="available"/> pixels when
    /// rendered with <paramref name="style"/>. Returns the input unchanged when it already fits, and
    /// a bare ellipsis when even the ellipsis is wider than the available width.
    /// </summary>
    public static string Truncate(ICanvas canvas, string text, TextStyle style, float available)
    {
        if (string.IsNullOrEmpty(text)) return text;
        if (available <= 0f) return string.Empty;
        if (canvas.MeasureTextWidth(text, style) <= available) return text;

        var ellipsisWidth = canvas.MeasureTextWidth(Ellipsis, style);
        if (ellipsisWidth > available) return Ellipsis;

        var lo = 0;
        var hi = text.Length;
        while (lo < hi)
        {
            var mid = TextBoundaries.Snap(text, (lo + hi + 1) / 2);
            if (mid <= lo)
                break;

            if (canvas.MeasureTextWidth(text.AsSpan(0, mid), style) + ellipsisWidth <= available)
                lo = mid;
            else
                hi = mid - 1;
        }
        return string.Concat(text.AsSpan(0, lo), Ellipsis);
    }

    /// <summary>
    /// <see cref="Truncate"/> from the other end: keeps as much of the end of <paramref name="text"/>
    /// as fits after a leading ellipsis.
    /// </summary>
    public static string TruncateStart(ICanvas canvas, string text, TextStyle style, float available)
    {
        if (string.IsNullOrEmpty(text)) return text;
        if (available <= 0f) return string.Empty;
        if (canvas.MeasureTextWidth(text, style) <= available) return text;

        var ellipsisWidth = canvas.MeasureTextWidth(Ellipsis, style);
        if (ellipsisWidth > available) return Ellipsis;

        var starts = new List<int>();
        for (var i = 0; i < text.Length; i += StringInfo.GetNextTextElementLength(text, i))
            starts.Add(i);
        starts.Add(text.Length);

        // The earliest cluster start whose suffix still fits; the empty suffix always does.
        var lo = 0;
        var hi = starts.Count - 1;
        while (lo < hi)
        {
            var mid = (lo + hi) / 2;
            if (canvas.MeasureTextWidth(text.AsSpan(starts[mid]), style) + ellipsisWidth <= available)
                hi = mid;
            else
                lo = mid + 1;
        }
        return string.Concat(Ellipsis, text.AsSpan(starts[hi]));
    }
}
