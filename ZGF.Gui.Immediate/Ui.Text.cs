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
}

public sealed partial class Ui : ITextLayout
{
    private Dictionary<LineKey, TextLines> _lines = new();
    private Dictionary<LineKey, TextLines> _previousLines = new();

    /// <summary>Width of <paramref name="text"/> shaped on its own — a slice of a longer string —
    /// as <see cref="UiPainter.Text(UiRect, string, int, int, uint, float?, bool, Align, string?)"/>
    /// draws it. Not cached.</summary>
    public float MeasureText(ReadOnlySpan<char> text, float? size = null, bool bold = false, string? family = null) =>
        text.IsEmpty ? 0f : MathF.Ceiling(_canvas.MeasureTextWidth(text, TextStyleFor(size, Theme.Text, bold, Align.Start, family)));

    TextLines ITextLayout.BreakLines(string text, TextStyle style, float maxWidth) => BreakLines(text, style, maxWidth);

    float ITextLayout.MeasureLineHeight(TextStyle style) => MeasureLineHeight(style);

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
            _previousLines.Clear();
        (_lines, _previousLines) = (_previousLines, _lines);
        _lines.Clear();
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

        var lineStart = start;
        while (lineStart < end)
        {
            var fit = -1;
            var fitWidth = 0f;
            var i = lineStart;
            while (i < end)
            {
                var wordEnd = i;
                while (wordEnd < end && text[wordEnd] != ' ') wordEnd++;
                var width = Measure(text, lineStart, wordEnd, style);
                if (width > maxWidth && fit >= 0) break;
                fit = wordEnd;
                fitWidth = width;
                if (width > maxWidth) break;
                i = wordEnd;
                while (i < end && text[i] == ' ') i++;
            }
            lines.Add(lineStart, fit - lineStart, fitWidth);
            lineStart = fit;
            while (lineStart < end && text[lineStart] == ' ') lineStart++;
        }
    }

    private float Measure(string text, int start, int end, TextStyle style) =>
        end <= start ? 0f : MathF.Ceiling(_canvas.MeasureTextWidth(text.AsSpan(start, end - start), style));

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
