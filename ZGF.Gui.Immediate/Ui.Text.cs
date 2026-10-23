using System.Runtime.CompilerServices;

namespace ZGF.Gui.Immediate;

/// <summary>Where a text breaks into lines: each line's start and length in the text, and the
/// widest line's width.</summary>
internal sealed class TextLines
{
    private int[] _starts = new int[2];
    private int[] _lengths = new int[2];

    public int Count { get; private set; }
    public float Width { get; private set; }

    public int Start(int line) => _starts[line];
    public int Length(int line) => _lengths[line];

    public void Add(int start, int length, float width)
    {
        if (Count == _starts.Length)
        {
            Array.Resize(ref _starts, Count * 2);
            Array.Resize(ref _lengths, Count * 2);
        }
        _starts[Count] = start;
        _lengths[Count] = length;
        Count++;
        Width = MathF.Max(Width, width);
    }
}

internal interface ITextLayout
{
    TextLines BreakLines(string text, TextStyle style, float maxWidth);
    float MeasureLineHeight(TextStyle style);
    string Ellipsize(string text, TextStyle style, float maxWidth, TextOverflow overflow);
    RichTextLayoutResult LayOutRuns(IReadOnlyList<RichTextRun> runs, float maxWidth);
}

public sealed partial class Ui : ITextLayout
{
    private readonly FrameCache<(StyledText, float), TextLines> _lines = new();
    private readonly FrameCache<(StyledText, float, TextOverflow), string> _ellipses = new();
    private readonly FrameCache<(RunList, float), RichTextLayoutResult> _runLines = new();
    private readonly List<Range> _ranges = new();

    /// <summary>Width of <paramref name="text"/> shaped on its own — a slice of a longer string —
    /// as <see cref="UiPainter.Text(UiRect, string, int, int, uint, float?, bool, Align, string?)"/>
    /// draws it. Not cached.</summary>
    public float MeasureText(ReadOnlySpan<char> text, float? size = null, bool bold = false, string? family = null) =>
        text.IsEmpty ? 0f : MathF.Ceiling(_canvas.MeasureTextWidth(text, TextStyleFor(size, Theme.Text, bold, Align.Start, family)));

    TextLines ITextLayout.BreakLines(string text, TextStyle style, float maxWidth) => BreakLines(text, style, maxWidth);

    float ITextLayout.MeasureLineHeight(TextStyle style) => MeasureLineHeight(style);

    string ITextLayout.Ellipsize(string text, TextStyle style, float maxWidth, TextOverflow overflow) =>
        Ellipsize(text, style, maxWidth, overflow);

    RichTextLayoutResult ITextLayout.LayOutRuns(IReadOnlyList<RichTextRun> runs, float maxWidth) => LayOutRuns(runs, maxWidth);

    /// <summary>The width <paramref name="runs"/> take unwrapped: their widest line, breaking only at
    /// '\n', as <see cref="RichText"/> lays them out. Kept by the list's identity.</summary>
    public float MeasureRuns(IReadOnlyList<RichTextRun> runs) => MathF.Ceiling(LayOutRuns(runs, 0f).MaxLineWidth);

    /// <summary>Lays <paramref name="runs"/> out into lines no wider than <paramref name="maxWidth"/>,
    /// or unwrapped when it is not positive. Kept while asked for this frame or the last, by the
    /// list's identity: pass the same list while the text is the same.</summary>
    internal RichTextLayoutResult LayOutRuns(IReadOnlyList<RichTextRun> runs, float maxWidth)
    {
        var key = (new RunList(runs), MathF.Floor(maxWidth));
        if (_runLines.TryGet(key, out var lines))
            return lines;
        return _runLines.Keep(key, RichTextLayout.Layout(_canvas, runs, key.Item2));
    }

    /// <summary><paramref name="text"/> as one line no wider than <paramref name="maxWidth"/>:
    /// itself when it fits, else shortened at its end or middle around an ellipsis, as
    /// <see cref="Text"/> shows it with an overflow. Kept while asked for this frame or the last, so
    /// a painter can call it every frame without allocating.</summary>
    public string Ellipsize(string text, float maxWidth, TextOverflow overflow = TextOverflow.Ellipsis, float? size = null, bool bold = false,
        string? family = null)
    {
        var style = TextStyleFor(size, Theme.Text, bold, Align.Start, family);
        return overflow == TextOverflow.Clip || MeasureWidth(text, style) <= maxWidth + 0.5f ? text : Ellipsize(text, style, maxWidth, overflow);
    }

    private string Ellipsize(string text, TextStyle style, float maxWidth, TextOverflow overflow)
    {
        var key = (new StyledText(text, style), MathF.Floor(maxWidth), overflow);
        if (_ellipses.TryGet(key, out var shown))
            return shown;
        return _ellipses.Keep(key, Shorten(text, style, MathF.Floor(maxWidth), overflow));
    }

    private const string EllipsisMark = "…";

    private string Shorten(string text, TextStyle style, float maxWidth, TextOverflow overflow)
    {
        var room = maxWidth - Measure(EllipsisMark, 0, EllipsisMark.Length, style);
        if (room < 0f) return "";
        var middle = overflow == TextOverflow.EllipsisMiddle;
        var low = 0;
        var high = text.Length;
        while (low < high)
        {
            var kept = (low + high + 1) / 2;
            if (KeptWidth(text, kept, middle, style) <= room)
                low = kept;
            else
                high = kept - 1;
        }
        var (head, tail) = Split(text, low, middle);
        return string.Concat(text.AsSpan(0, head).TrimEnd(), EllipsisMark, text.AsSpan(text.Length - tail).TrimStart());
    }

    private float KeptWidth(string text, int kept, bool middle, TextStyle style)
    {
        var (head, tail) = Split(text, kept, middle);
        return Measure(text, 0, head, style) + Measure(text, text.Length - tail, text.Length, style);
    }

    private static (int Head, int Tail) Split(string text, int kept, bool middle)
    {
        var cut = middle ? (kept + 1) / 2 : kept;
        var head = TextBoundaries.Snap(text, cut);
        if (!middle)
            return (head, 0);
        var tailStart = TextBoundaries.Snap(text, text.Length - (kept - head));
        return (head, text.Length - tailStart);
    }

    /// <summary>Breaks <paramref name="text"/> at newlines and, where a line is wider than
    /// <paramref name="maxWidth"/>, between words. Kept while asked for this frame or the last.</summary>
    internal TextLines BreakLines(string text, TextStyle style, float maxWidth)
    {
        var key = (new StyledText(text, style), maxWidth);
        if (_lines.TryGet(key, out var lines))
            return lines;
        return _lines.Keep(key, Break(text, style, maxWidth));
    }

    private void BeginLines(bool invalidate)
    {
        _lines.BeginFrame(invalidate);
        _ellipses.BeginFrame(invalidate);
        _runLines.BeginFrame(invalidate);
    }

    private TextLines Break(string text, TextStyle style, float maxWidth)
    {
        var lines = new TextLines();
        var start = 0;
        while (true)
        {
            var newline = text.IndexOf('\n', start);
            var end = newline < 0 ? text.Length : newline;
            if (end > start && text[end - 1] == '\r') end--;
            Wrap(lines, text, start, end, style, maxWidth);
            if (newline < 0) break;
            start = newline + 1;
        }
        return lines;
    }

    private void Wrap(TextLines lines, string text, int start, int end, TextStyle style, float maxWidth)
    {
        if (start == end)
        {
            lines.Add(start, 0, 0f);
            return;
        }
        if (float.IsPositiveInfinity(maxWidth))
        {
            lines.Add(start, end - start, Measure(text, start, end, style));
            return;
        }

        _ranges.Clear();
        TextWrapper.WrapRanges(_canvas, text.AsSpan(start, end - start), style, maxWidth, _ranges);
        foreach (var range in _ranges)
        {
            var lineStart = start + range.Start.Value;
            var lineEnd = start + range.End.Value;
            while (lineEnd > lineStart && text[lineEnd - 1] == ' ') lineEnd--;
            lines.Add(lineStart, lineEnd - lineStart, Measure(text, lineStart, lineEnd, style));
        }
    }

    private float Measure(string text, int start, int end, TextStyle style) =>
        end <= start ? 0f : MathF.Ceiling(_canvas.MeasureTextWidth(text.AsSpan(start, end - start), style));

    /// <summary>A list of runs as a cache key, by identity.</summary>
    private readonly record struct RunList(IReadOnlyList<RichTextRun> Runs)
    {
        public bool Equals(RunList other) => ReferenceEquals(Runs, other.Runs);

        public override int GetHashCode() => RuntimeHelpers.GetHashCode(Runs);
    }

    /// <summary>A text in a style as a cache key; styles are interned, so they compare by reference.</summary>
    private readonly record struct StyledText(string Text, TextStyle Style)
    {
        public bool Equals(StyledText other) => ReferenceEquals(Style, other.Style) && string.Equals(Text, other.Text);

        public override int GetHashCode() => HashCode.Combine(Text, RuntimeHelpers.GetHashCode(Style));
    }
}
