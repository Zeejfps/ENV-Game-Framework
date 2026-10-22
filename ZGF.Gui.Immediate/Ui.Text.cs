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
}

public sealed partial class Ui : ITextLayout
{
    private Dictionary<LineKey, TextLines> _lines = new();
    private Dictionary<LineKey, TextLines> _previousLines = new();
    private Dictionary<EllipsisKey, string> _ellipses = new();
    private Dictionary<EllipsisKey, string> _previousEllipses = new();
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
        var key = new EllipsisKey(text, style, MathF.Floor(maxWidth), overflow);
        if (_ellipses.TryGetValue(key, out var shown))
            return shown;
        if (!_previousEllipses.TryGetValue(key, out shown!))
            shown = Shorten(text, style, MathF.Floor(maxWidth), overflow);
        _ellipses[key] = shown;
        return shown;
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
        var head = middle ? (kept + 1) / 2 : kept;
        if (head > 0 && head < text.Length && char.IsLowSurrogate(text[head])) head--;
        var tail = middle ? kept - head : 0;
        if (tail > 0 && char.IsLowSurrogate(text[text.Length - tail])) tail--;
        return (head, tail);
    }

    /// <summary>Breaks <paramref name="text"/> at newlines and, where a line is wider than
    /// <paramref name="maxWidth"/>, between words. Kept while asked for this frame or the last.</summary>
    internal TextLines BreakLines(string text, TextStyle style, float maxWidth)
    {
        var key = new LineKey(text, style, maxWidth);
        if (_lines.TryGetValue(key, out var lines))
            return lines;
        if (!_previousLines.TryGetValue(key, out lines))
            lines = Break(text, style, maxWidth);
        _lines[key] = lines;
        return lines;
    }

    private void BeginLines(bool invalidate)
    {
        if (invalidate)
        {
            _previousLines.Clear();
            _previousEllipses.Clear();
        }
        (_lines, _previousLines) = (_previousLines, _lines);
        _lines.Clear();
        (_ellipses, _previousEllipses) = (_previousEllipses, _ellipses);
        _ellipses.Clear();
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

    private readonly record struct EllipsisKey(string Text, TextStyle Style, float Width, TextOverflow Overflow) : IEquatable<EllipsisKey>
    {
        public bool Equals(EllipsisKey other) =>
            ReferenceEquals(Style, other.Style) && Width.Equals(other.Width) && Overflow == other.Overflow && string.Equals(Text, other.Text);

        public override int GetHashCode() =>
            HashCode.Combine(Text, System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(Style), Width, Overflow);
    }

    private readonly struct LineKey : IEquatable<LineKey>
    {
        private readonly string _text;
        private readonly TextStyle _style;
        private readonly float _width;

        public LineKey(string text, TextStyle style, float width)
        {
            _text = text;
            _style = style;
            _width = width;
        }

        public bool Equals(LineKey other) =>
            ReferenceEquals(_style, other._style) && _width.Equals(other._width) && string.Equals(_text, other._text);

        public override bool Equals(object? obj) => obj is LineKey other && Equals(other);

        public override int GetHashCode() =>
            HashCode.Combine(_text, System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(_style), _width);
    }
}
