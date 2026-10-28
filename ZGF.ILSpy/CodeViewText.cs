using System.Text;
using ZGF.Gui;
using ZGF.ILSpy.Core;

namespace ZGF.ILSpy;

internal static class CodeText
{
    public static string Between(DecompiledDocument document, TextPosition start, TextPosition end)
    {
        var lines = document.Lines;
        if (lines.Length == 0 || start >= end) return "";
        start = Clamp(document, start);
        end = Clamp(document, end);
        var first = lines[start.Line].Text;
        if (start.Line == end.Line)
            return first.Substring(start.Column, end.Column - start.Column);

        var builder = new StringBuilder();
        builder.Append(first, start.Column, first.Length - start.Column).Append('\n');
        if (end.Line - start.Line > 1)
            builder.Append(document.GetText(start.Line + 1, end.Line)).Append('\n');
        builder.Append(lines[end.Line].Text, 0, end.Column);
        return builder.ToString();
    }

    public static TextPosition Clamp(DecompiledDocument document, TextPosition position)
    {
        var lines = document.Lines;
        if (lines.Length == 0) return default;
        var line = Math.Clamp(position.Line, 0, lines.Length - 1);
        return new TextPosition(line, Math.Clamp(position.Column, 0, lines[line].Text.Length));
    }

    public static TextPosition End(DecompiledDocument document) =>
        document.Lines.Length == 0 ? default : new TextPosition(document.Lines.Length - 1, document.Lines[^1].Text.Length);

    public static (TextPosition Start, TextPosition End) WordAt(DecompiledDocument document, TextPosition at)
    {
        TextBoundaries.WordAt(document.Lines[at.Line].Text, at.Column, out var start, out var end, splitPunctuation: true);
        return (at with { Column = start }, at with { Column = end });
    }

    public static int SpanAt(ReadOnlySpan<DocumentSpan> spans, int column)
    {
        int low = 0, high = spans.Length - 1;
        while (low <= high)
        {
            var mid = (low + high) >> 1;
            if (spans[mid].End <= column) low = mid + 1;
            else if (spans[mid].Start > column) high = mid - 1;
            else return mid;
        }
        return -1;
    }

    public static int Digits(int value)
    {
        var digits = 1;
        while (value >= 10)
        {
            value /= 10;
            digits++;
        }
        return digits;
    }
}
