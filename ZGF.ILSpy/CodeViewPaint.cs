using ZGF.Geometry;
using ZGF.Gui.Immediate;
using ZGF.ILSpy.Core;

namespace ZGF.ILSpy;

/// <summary>What every row of one frame of a code view is painted from, refilled each frame.</summary>
internal sealed class CodeViewFrame
{
    private int[] _codePoints = new int[256];

    public DecompiledDocument Document = DecompiledDocument.Empty;
    public CodeFolds Folds = new();
    public CodeViewPalette Palette = BuiltInThemes.Dark.Code;
    public uint Background;
    public uint GutterHover;
    public uint Selection;
    public uint ErrorText;
    public uint Flash;
    public float FontSize;
    public float SeparatorWidth;
    public float OutlineWidth;
    public float ChipRadius;
    public float GutterPaddingLeft;
    public float Advance;
    public float RowHeight;
    public float CellHeight;
    public float FoldLeft;
    public float FoldWidth;
    public float GutterWidth;
    public float TextLeft;
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
    public int HoverFold = -1;
    public int HoverChip = -1;
    public float MarkWidth;
    public float IconSize;
    public IconGlyph BookmarkGlyph;
    public IReadOnlyDictionary<int, bool>? Bookmarks;
    public int HoverMark = -1;

    /// <summary>The caret's line when it is highlighted, else -1.</summary>
    public int CurrentLine = -1;

    /// <summary>The bracket at the caret and its match when they are highlighted.</summary>
    public BracketPair? Brackets;

    public static void PaintRow(UiPainter p, (CodeViewFrame Frame, int Row) row) => row.Frame.Paint(p, row.Row);

    private void Paint(UiPainter p, int row)
    {
        var r = p.Rect;
        var left = p.Clip.X;
        var right = MathF.Min(r.Right, p.Clip.Right);
        var index = Folds.LineAt(row);
        var line = Document.Lines[index];
        var text = line.Text;
        var fold = Document.FoldStartingAt(index);
        var collapsed = fold >= 0 && Folds.IsCollapsed(fold);
        var chipStart = text.Length + 1;
        var chipText = collapsed ? Document.Folds[fold].CollapsedText : "";
        var x0 = r.X + TextLeft;
        var textAreaLeft = left + GutterWidth;
        var firstVisible = Math.Max(0, (int)MathF.Floor((textAreaLeft - x0) / Advance));
        var endVisible = Math.Min(text.Length, (int)MathF.Ceiling((right - x0) / Advance));
        var cellTop = r.Y + MathF.Floor((RowHeight - CellHeight) / 2);

        if (index == CurrentLine)
            p.FillRect(new UiRect(textAreaLeft, r.Y, right - textAreaLeft, r.Height), Palette.CurrentLine);

        if (index == FlashLine)
            p.FillRect(new UiRect(textAreaLeft, r.Y, right - textAreaLeft, r.Height), Flash);

        if (Brackets != null)
            FillBrackets(p, x0, textAreaLeft, r, index, Brackets.Value);

        if (Highlight != null)
        {
            foreach (var span in line.Spans)
            {
                if (span.Reference != null && span.Reference.Equals(Highlight))
                    Fill(p, x0, textAreaLeft, r, span.Start, span.End, Palette.Occurrence);
            }
        }

        if (MatchLength > 0)
        {
            for (var m = FirstMatchOn(index); m < Matches.Count && Matches[m].Line == index; m++)
            {
                if (m == CurrentMatch)
                    Fill(p, x0, textAreaLeft, r, Matches[m].Column, Matches[m].Column + MatchLength, Palette.CurrentFind, Palette.CurrentFindOutline, OutlineWidth);
                else
                    Fill(p, x0, textAreaLeft, r, Matches[m].Column, Matches[m].Column + MatchLength, Palette.FindMatch);
            }
        }

        if (SelectionStart < SelectionEnd && index >= SelectionStart.Line && index <= SelectionEnd.Line)
        {
            var from = index == SelectionStart.Line ? SelectionStart.Column : 0;
            var to = index == SelectionEnd.Line ? SelectionEnd.Column
                : collapsed && SelectionEnd.Line > Document.Folds[fold].EndLine ? chipStart + chipText.Length + 1
                : text.Length + 1;
            Fill(p, x0, textAreaLeft, r, from, to, Selection);
        }

        if (collapsed)
        {
            var chipLeft = x0 + chipStart * Advance;
            p.FillRect(new UiRect(chipLeft - Advance / 2, cellTop, (chipText.Length + 1) * Advance, CellHeight),
                fold == HoverChip ? Palette.FoldChipHovered : Palette.FoldChip, ChipRadius);
            p.GlyphRun(new PointF(chipLeft, cellTop), Cells(chipText, 0, chipText.Length), Advance, fold == HoverChip ? GutterHover : Palette.GutterText,
                FontSize, family: MonoFonts.Code);
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
                p.GlyphRun(new PointF(x0 + start * Advance, cellTop), cells, Advance, error ? ErrorText : Palette[span.Kind],
                    FontSize, family: MonoFonts.Code, underline: index == UnderlineLine && s == UnderlineSpan);
            }
        }

        if (GutterWidth <= 0f) return;
        p.FillRect(new UiRect(left, r.Y, GutterWidth, r.Height), Background);
        p.FillRect(new UiRect(left + GutterWidth - SeparatorWidth, r.Y, SeparatorWidth, r.Height), Palette.GutterSeparator);
        if (Digits > 0)
            LineNumber(p, left, cellTop, index);
        if (Bookmarks != null && (Bookmarks.TryGetValue(index, out var enabled) || index == HoverMark))
        {
            var color = !Bookmarks.ContainsKey(index) ? Palette.GutterText : enabled ? Palette.Bookmark : Palette.GutterText;
            var size = MathF.Min(IconSize, RowHeight);
            p.Glyph(new UiRect(left + (MarkWidth - size) / 2, r.Y + (r.Height - size) / 2, size, size), BookmarkGlyph, color, size);
        }
        if (fold >= 0 && FoldWidth > 0f)
            Chevron(p, left + FoldLeft + FoldWidth / 2, r.Y + r.Height / 2, collapsed, fold == HoverFold ? GutterHover : Palette.GutterText);
    }

    /// <summary>The line's number, right-aligned, brighter on the highlighted current line.</summary>
    private void LineNumber(UiPainter p, float left, float cellTop, int index)
    {
        var number = Number(index + 1);
        var color = index == CurrentLine ? GutterHover : Palette.GutterText;
        var x = left + GutterPaddingLeft + (Digits - number.Length) * Advance;
        p.GlyphRun(new PointF(x, cellTop), number, Advance, color, FontSize, family: MonoFonts.Code);
    }

    private void FillBrackets(UiPainter p, float x0, float left, UiRect row, int index, BracketPair brackets)
    {
        if (brackets.Open.Line == index)
            Fill(p, x0, left, row, brackets.Open.Column, brackets.Open.Column + 1, Palette.MatchingBracket);
        if (brackets.Close.Line == index)
            Fill(p, x0, left, row, brackets.Close.Column, brackets.Close.Column + 1, Palette.MatchingBracket);
    }

    private const float ChevronStroke = 1.5f;

    private void Chevron(UiPainter p, float cx, float cy, bool collapsed, uint color)
    {
        var half = MathF.Max(2f, MathF.Floor(Advance * 0.4f));
        p.DrawDisclosure(new PointF(cx, cy), half, !collapsed, color, ChevronStroke);
    }

    private void Fill(UiPainter p, float x0, float left, UiRect row, int from, int to, uint color, uint outline = 0, float outlineWidth = 0f)
    {
        var x = MathF.Max(left, x0 + from * Advance);
        var right = x0 + to * Advance;
        if (right > x)
            p.FillRect(new UiRect(x, row.Y, right - x, row.Height), color, 0f, outline, outlineWidth);
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
