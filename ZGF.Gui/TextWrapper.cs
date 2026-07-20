using System.Text;

namespace ZGF.Gui;

/// <summary>
/// Greedy line-wrap that uses an <see cref="ICanvas"/> to measure widths in the supplied
/// <see cref="TextStyle"/>. Latin runs break at spaces and after separator punctuation, so paths,
/// URLs and snake_case identifiers wrap at their segment boundaries; CJK and other "wide" scripts
/// also break between adjacent code points (UAX-14-lite). A run with no break opportunity that is
/// still wider than the line breaks between code points rather than overflowing. Minimal kinsoku
/// keeps closing punctuation off the start of a line and opening punctuation off the end.
/// </summary>
public static class TextWrapper
{
    /// <summary>
    /// Wraps <paramref name="text"/> into <paramref name="output"/>. Each newline in the input
    /// starts a new visual line. Lines that already fit are appended unmodified.
    /// </summary>
    public static void Wrap(ICanvas canvas, string text, TextStyle style, float maxWidth, List<string> output)
    {
        if (text.Length == 0)
        {
            output.Add(string.Empty);
            return;
        }

        var rawLines = text.Replace("\r\n", "\n").Split('\n');
        foreach (var raw in rawLines)
        {
            WrapLine(canvas, raw, style, maxWidth, output);
        }
    }

    /// <summary>
    /// Wraps <paramref name="text"/> into ranges over the input rather than new strings, for callers
    /// that index the text they wrapped (a text field's caret is a UTF-16 index into its buffer).
    /// The ranges tile the input: a soft-wrapped line ends where the next begins — spaces at the break
    /// stay on the line they follow, so every index remains reachable by the caret — while a newline
    /// leaves a one-character gap between them.
    /// </summary>
    public static void WrapRanges(ICanvas canvas, ReadOnlySpan<char> text, TextStyle style, float maxWidth, List<Range> output)
    {
        var lineStart = 0;
        for (var i = 0; i <= text.Length; i++)
        {
            if (i != text.Length && text[i] != '\n')
                continue;

            WrapLineRanges(canvas, text, lineStart, i, style, maxWidth, output);
            lineStart = i + 1;
        }
    }

    private static void WrapLineRanges(
        ICanvas canvas, ReadOnlySpan<char> text, int start, int end, TextStyle style, float maxWidth, List<Range> output)
    {
        var raw = text[start..end];
        if (raw.Length == 0 || maxWidth <= 0f || canvas.MeasureTextWidth(raw, style) <= maxWidth)
        {
            output.Add(new Range(start, end));
            return;
        }

        var spaceWidth = canvas.MeasureTextWidth(" ", style);
        var lineStart = start;
        var lineWidth = 0f;
        var lineHasContent = false;

        var i = 0;
        var n = raw.Length;
        while (i < n)
        {
            var spaces = 0;
            while (i < n && raw[i] == ' ')
            {
                i++;
                spaces++;
            }
            if (i >= n)
                break;

            var chunkStart = i;
            var prev = ReadCodePoint(raw, ref i);
            while (i < n && raw[i] != ' ')
            {
                var next = ReadCodePoint(raw, i, out var nextLen);
                if (BreakAllowedBetween(prev, next))
                    break;
                prev = next;
                i += nextLen;
            }

            var chunkWidth = canvas.MeasureTextWidth(raw[chunkStart..i], style);
            var sep = spaces * spaceWidth;

            if (chunkWidth > maxWidth)
            {
                // Mirrors WrapLine: an unbreakable over-wide chunk starts a fresh line and is split
                // between code points. The two variants must agree or the caret lands off-line.
                if (lineHasContent)
                {
                    output.Add(new Range(lineStart, start + chunkStart));
                    lineStart = start + chunkStart;
                    lineWidth = 0f;
                }
                else
                {
                    lineWidth += sep;
                }

                var j = chunkStart;
                var before = -1;
                while (j < i)
                {
                    var cur = ReadCodePoint(raw, j, out var len);
                    var w = canvas.MeasureTextWidth(raw.Slice(j, len), style);
                    if (start + j > lineStart && lineWidth + w > maxWidth && BreakAllowedHere(before, cur))
                    {
                        output.Add(new Range(lineStart, start + j));
                        lineStart = start + j;
                        lineWidth = 0f;
                    }
                    lineWidth += w;
                    before = cur;
                    j += len;
                }

                lineHasContent = true;
            }
            else if (!lineHasContent)
            {
                lineWidth += sep + chunkWidth;
                lineHasContent = true;
            }
            else if (lineWidth + sep + chunkWidth <= maxWidth)
            {
                lineWidth += sep + chunkWidth;
            }
            else
            {
                output.Add(new Range(lineStart, start + chunkStart));
                lineStart = start + chunkStart;
                lineWidth = chunkWidth;
            }
        }

        output.Add(new Range(lineStart, end));
    }

    private static void WrapLine(ICanvas canvas, string raw, TextStyle style, float maxWidth, List<string> output)
    {
        if (raw.Length == 0)
        {
            output.Add(string.Empty);
            return;
        }
        if (maxWidth <= 0f || canvas.MeasureTextWidth(raw, style) <= maxWidth)
        {
            output.Add(raw);
            return;
        }

        // Width is accumulated per chunk rather than re-measuring the whole candidate each step:
        // a spaceless CJK line is one chunk per code point, so re-measuring would be quadratic.
        // Summing chunk widths slightly over-estimates (it drops cross-chunk kerning), which only
        // ever wraps earlier — it never overflows.
        var spaceWidth = canvas.MeasureTextWidth(" ", style);
        var line = new StringBuilder();
        var lineWidth = 0f;

        var i = 0;
        var n = raw.Length;
        var spaceBefore = false;
        while (i < n)
        {
            if (raw[i] == ' ')
            {
                spaceBefore = true;
                do { i++; } while (i < n && raw[i] == ' ');
                continue;
            }

            var start = i;
            var prev = ReadCodePoint(raw, ref i);
            while (i < n && raw[i] != ' ')
            {
                var next = ReadCodePoint(raw, i, out var nextLen);
                if (BreakAllowedBetween(prev, next))
                    break;
                prev = next;
                i += nextLen;
            }

            var chunk = raw.AsSpan(start, i - start);
            var chunkWidth = canvas.MeasureTextWidth(chunk, style);

            if (chunkWidth > maxWidth)
            {
                // Nothing inside this chunk is a break opportunity, yet it can't fit on a line of its
                // own. Start it fresh and split it between code points rather than overflow.
                if (line.Length > 0)
                {
                    output.Add(line.ToString());
                    line.Clear();
                    lineWidth = 0f;
                }
                AppendBrokenChunk(canvas, chunk, style, maxWidth, line, ref lineWidth, output);
            }
            else if (line.Length == 0)
            {
                line.Append(chunk);
                lineWidth = chunkWidth;
            }
            else
            {
                var sep = spaceBefore ? spaceWidth : 0f;
                if (lineWidth + sep + chunkWidth <= maxWidth)
                {
                    if (spaceBefore)
                    {
                        line.Append(' ');
                        lineWidth += spaceWidth;
                    }
                    line.Append(chunk);
                    lineWidth += chunkWidth;
                }
                else
                {
                    output.Add(line.ToString());
                    line.Clear();
                    line.Append(chunk);
                    lineWidth = chunkWidth;
                }
            }

            spaceBefore = false;
        }

        if (line.Length > 0) output.Add(line.ToString());
    }

    /// <summary>
    /// Fills lines with <paramref name="chunk"/> a code point at a time, flushing each full line to
    /// <paramref name="output"/> and leaving the trailing partial line in <paramref name="line"/>.
    /// Kinsoku still applies, so a cluster the engine glued together (a closing mark after its
    /// ideograph) overflows rather than being torn apart here.
    /// </summary>
    private static void AppendBrokenChunk(
        ICanvas canvas,
        ReadOnlySpan<char> chunk,
        TextStyle style,
        float maxWidth,
        StringBuilder line,
        ref float lineWidth,
        List<string> output)
    {
        var i = 0;
        var prev = -1;
        while (i < chunk.Length)
        {
            var cur = ReadCodePoint(chunk, i, out var len);
            var cp = chunk.Slice(i, len);
            var w = canvas.MeasureTextWidth(cp, style);

            if (line.Length > 0 && lineWidth + w > maxWidth && BreakAllowedHere(prev, cur))
            {
                output.Add(line.ToString());
                line.Clear();
                lineWidth = 0f;
            }

            line.Append(cp);
            lineWidth += w;
            prev = cur;
            i += len;
        }
    }

    /// <summary>
    /// Whether a last-resort character break may fall between two code points. Unlike
    /// <see cref="BreakAllowedBetween"/> it needs no break opportunity — only the absence of a
    /// kinsoku prohibition. <paramref name="before"/> is -1 at the start of a run.
    /// </summary>
    private static bool BreakAllowedHere(int before, int after) =>
        before >= 0 && !IsNoBreakBefore(after) && !IsNoBreakAfter(before);

    private static int ReadCodePoint(ReadOnlySpan<char> s, ref int i)
    {
        var cp = ReadCodePoint(s, i, out var len);
        i += len;
        return cp;
    }

    private static int ReadCodePoint(ReadOnlySpan<char> s, int i, out int len)
    {
        var c = s[i];
        if (char.IsHighSurrogate(c) && i + 1 < s.Length && char.IsLowSurrogate(s[i + 1]))
        {
            len = 2;
            return char.ConvertToUtf32(c, s[i + 1]);
        }

        len = 1;
        return c;
    }

    private static bool BreakAllowedBetween(int before, int after)
    {
        if (IsNoBreakBefore(after)) return false;  // kinsoku: closing punctuation can't start a line
        if (IsNoBreakAfter(before)) return false;  // kinsoku: opening punctuation can't end a line
        return IsWide(before) || IsWide(after) || IsBreakAfter(before);
    }

    /// <summary>
    /// Separators that permit a break on their trailing side, so the separator stays with the segment
    /// it follows. Lets spaceless runs — paths, URLs, hyphenated and snake_case identifiers — wrap at
    /// their natural boundaries.
    /// </summary>
    private static bool IsBreakAfter(int cp) => cp switch
    {
        '/' or '\\' or '-' or '_' or '.' or ':' => true,
        _ => false,
    };

    /// <summary>
    /// CJK and other ideographic / syllabic code points that permit a break on either side
    /// (UAX-14 ID-like classes). Latin/Cyrillic/etc. return false, so they keep word-only breaking.
    /// </summary>
    public static bool IsWide(int cp) =>
        (cp >= 0x1100 && cp <= 0x11FF) ||   // Hangul Jamo
        (cp >= 0x2E80 && cp <= 0x2EFF) ||   // CJK Radicals Supplement
        (cp >= 0x2F00 && cp <= 0x2FDF) ||   // Kangxi Radicals
        (cp >= 0x3000 && cp <= 0x303F) ||   // CJK Symbols and Punctuation
        (cp >= 0x3040 && cp <= 0x309F) ||   // Hiragana
        (cp >= 0x30A0 && cp <= 0x30FF) ||   // Katakana
        (cp >= 0x3100 && cp <= 0x312F) ||   // Bopomofo
        (cp >= 0x3130 && cp <= 0x318F) ||   // Hangul Compatibility Jamo
        (cp >= 0x31F0 && cp <= 0x31FF) ||   // Katakana Phonetic Extensions
        (cp >= 0x3200 && cp <= 0x32FF) ||   // Enclosed CJK Letters and Months
        (cp >= 0x3400 && cp <= 0x4DBF) ||   // CJK Unified Ideographs Extension A
        (cp >= 0x4E00 && cp <= 0x9FFF) ||   // CJK Unified Ideographs
        (cp >= 0xA960 && cp <= 0xA97F) ||   // Hangul Jamo Extended-A
        (cp >= 0xAC00 && cp <= 0xD7A3) ||   // Hangul Syllables
        (cp >= 0xD7B0 && cp <= 0xD7FF) ||   // Hangul Jamo Extended-B
        (cp >= 0xF900 && cp <= 0xFAFF) ||   // CJK Compatibility Ideographs
        (cp >= 0xFF00 && cp <= 0xFFEF) ||   // Halfwidth and Fullwidth Forms
        (cp >= 0x20000 && cp <= 0x2FA1F);   // CJK Unified Ideographs Ext B-F + Compatibility Supplement

    /// <summary>Characters that must not begin a wrapped line (closing punctuation, small kana).</summary>
    private static bool IsNoBreakBefore(int cp) => cp switch
    {
        ',' or '.' or '!' or '?' or ':' or ';' or ')' or ']' or '}' => true,
        0x3001 or 0x3002 or 0x3009 or 0x300B or 0x300D or 0x300F or 0x3011
            or 0x3015 or 0x3017 or 0x3019 or 0x301B => true,  // 、。〉》」』】〕〗〙〛
        0x3005 or 0x301C or 0x30FC => true,                   // 々〜ー
        0x2025 or 0x2026 => true,                             // ‥…
        0x3041 or 0x3043 or 0x3045 or 0x3047 or 0x3049 or 0x3063
            or 0x3083 or 0x3085 or 0x3087 or 0x308E or 0x3095 or 0x3096 => true,  // small hiragana
        0x30A1 or 0x30A3 or 0x30A5 or 0x30A7 or 0x30A9 or 0x30C3
            or 0x30E3 or 0x30E5 or 0x30E7 or 0x30EE or 0x30F5 or 0x30F6 => true,  // small katakana
        0xFF01 or 0xFF09 or 0xFF0C or 0xFF0E or 0xFF1A or 0xFF1B
            or 0xFF1F or 0xFF3D or 0xFF5D or 0xFF63 => true,  // ！），．：；？］｝｣
        _ => false,
    };

    /// <summary>Characters that must not end a line (opening punctuation).</summary>
    private static bool IsNoBreakAfter(int cp) => cp switch
    {
        '(' or '[' or '{' => true,
        0x3008 or 0x300A or 0x300C or 0x300E or 0x3010
            or 0x3014 or 0x3016 or 0x3018 or 0x301A => true,  // 〈《「『【〔〖〘〚
        0xFF08 or 0xFF3B or 0xFF5B or 0xFF62 => true,         // （［｛｢
        _ => false,
    };
}
