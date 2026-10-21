using ZGF.Geometry;
using ZGF.Gui.Immediate;
using ZGF.ILSpy.Core;

namespace ZGF.ILSpy;

/// <summary>What every row of one frame of a code view is painted from, refilled each frame.</summary>
internal sealed class CodeViewFrame
{
    private int[] _codePoints = new int[256];

    public DecompiledDocument Document = DecompiledDocument.Empty;
    public CodeViewPalette Palette = CodeViewPalette.Dark;
    public uint Background;
    public uint GutterText;
    public uint GutterBorder;
    public uint Selection;
    public uint Occurrence;
    public uint Flash;
    public float Advance;
    public float RowHeight;
    public float CellHeight;
    public float GutterWidth;
    public float TextLeft;
    public float ScrollX;
    public int Digits;
    public TextPosition SelectionStart;
    public TextPosition SelectionEnd;
    public CodeReference? Highlight;
    public int UnderlineLine = -1;
    public int UnderlineSpan = -1;
    public IReadOnlyList<TextPosition> Matches = [];
    public int MatchLength;
    public int CurrentMatch = -1;
    public int FlashLine = -1;

    /// <summary>The width rows were last painted at, which the next frame scrolls against.</summary>
    public float ViewWidth;

    public static void PaintRow(UiPainter p, (CodeViewFrame Frame, int Line) row) => row.Frame.Paint(p, row.Line);

    private void Paint(UiPainter p, int index)
    {
        var r = p.Rect;
        ViewWidth = r.Width;
        var line = Document.Lines[index];
        var text = line.Text;
        var x0 = r.X + TextLeft - ScrollX;
        var textAreaLeft = r.X + GutterWidth;
        var firstVisible = Math.Max(0, (int)MathF.Floor((textAreaLeft - x0) / Advance));
        var endVisible = Math.Min(text.Length, (int)MathF.Ceiling((r.Right - x0) / Advance));
        var cellTop = r.Y + MathF.Floor((RowHeight - CellHeight) / 2);

        if (index == FlashLine)
            p.FillRect(new UiRect(textAreaLeft, r.Y, r.Right - textAreaLeft, r.Height), Flash);

        if (Highlight is { } highlight)
        {
            foreach (var span in line.Spans)
            {
                if (span.Reference is { } reference && reference.Equals(highlight))
                    Fill(p, x0, textAreaLeft, r, span.Start, span.End, Occurrence);
            }
        }

        if (MatchLength > 0)
        {
            for (var m = FirstMatchOn(index); m < Matches.Count && Matches[m].Line == index; m++)
                Fill(p, x0, textAreaLeft, r, Matches[m].Column, Matches[m].Column + MatchLength, m == CurrentMatch ? Palette.CurrentMatch : Palette.Match);
        }

        if (SelectionStart < SelectionEnd && index >= SelectionStart.Line && index <= SelectionEnd.Line)
        {
            var from = index == SelectionStart.Line ? SelectionStart.Column : 0;
            var to = index == SelectionEnd.Line ? SelectionEnd.Column : text.Length + 1;
            Fill(p, x0, textAreaLeft, r, from, to, Selection);
        }

        if (endVisible > firstVisible)
        {
            var spans = line.Spans;
            var error = Document.IsError;
            var first = Math.Max(0, CodeText.SpanAt(spans, firstVisible));
            for (var s = first; s < spans.Length && spans[s].Start < endVisible; s++)
            {
                var span = spans[s];
                var start = Math.Max(span.Start, firstVisible);
                var end = Math.Min(span.End, endVisible);
                if (end <= start) continue;
                var cells = Cells(text, start, end);
                p.GlyphRun(new PointF(x0 + start * Advance, cellTop), cells, Advance, error ? Palette.Error : Palette[span.Kind],
                    family: MonoFonts.Regular, underline: index == UnderlineLine && s == UnderlineSpan);
            }
        }

        p.FillRect(new UiRect(r.X, r.Y, GutterWidth, r.Height), Background);
        p.FillRect(new UiRect(r.X + GutterWidth - 1f, r.Y, 1f, r.Height), GutterBorder);
        var number = Number(index + 1);
        p.GlyphRun(new PointF(r.X + (Digits + 1 - number.Length) * Advance, cellTop), number, Advance, GutterText, family: MonoFonts.Regular);
    }

    private void Fill(UiPainter p, float x0, float left, UiRect row, int from, int to, uint color)
    {
        var x = MathF.Max(left, x0 + from * Advance);
        var right = MathF.Min(row.Right, x0 + to * Advance);
        if (right > x)
            p.FillRect(new UiRect(x, row.Y, right - x, row.Height), color);
    }

    private int FirstMatchOn(int line)
    {
        int low = 0, high = Matches.Count;
        while (low < high)
        {
            var mid = (low + high) >> 1;
            if (Matches[mid].Line < line) low = mid + 1;
            else high = mid;
        }
        return low;
    }

    private ReadOnlySpan<int> Cells(string text, int start, int end)
    {
        var count = end - start;
        if (_codePoints.Length < count)
            _codePoints = new int[Math.Max(count, _codePoints.Length * 2)];
        for (var i = 0; i < count; i++)
        {
            var at = start + i;
            var c = text[at];
            if (char.IsHighSurrogate(c) && at + 1 < text.Length && char.IsLowSurrogate(text[at + 1]))
            {
                _codePoints[i] = char.ConvertToUtf32(c, text[at + 1]);
                if (i + 1 < count) _codePoints[++i] = ' ';
            }
            else
            {
                _codePoints[i] = char.IsSurrogate(c) ? ' ' : c;
            }
        }
        return _codePoints.AsSpan(0, count);
    }

    private ReadOnlySpan<int> Number(int value)
    {
        var digits = CodeText.Digits(value);
        var cells = _codePoints.AsSpan(0, digits);
        for (var i = digits - 1; i >= 0; i--)
        {
            cells[i] = '0' + value % 10;
            value /= 10;
        }
        return cells;
    }
}
