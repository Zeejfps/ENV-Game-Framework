using System.Text;
using ZGF.ILSpy.Core;

namespace ZGF.ILSpy;

/// <summary>A caret position in a <see cref="DecompiledDocument"/>: a line and a UTF-16 column,
/// which is also the cell the code view draws it in.</summary>
public readonly record struct TextPosition(int Line, int Column) : IComparable<TextPosition>
{
    public int CompareTo(TextPosition other) => Line != other.Line ? Line.CompareTo(other.Line) : Column.CompareTo(other.Column);

    public static bool operator <(TextPosition a, TextPosition b) => a.CompareTo(b) < 0;
    public static bool operator >(TextPosition a, TextPosition b) => a.CompareTo(b) > 0;
    public static bool operator <=(TextPosition a, TextPosition b) => a.CompareTo(b) <= 0;
    public static bool operator >=(TextPosition a, TextPosition b) => a.CompareTo(b) >= 0;

    public static TextPosition Min(TextPosition a, TextPosition b) => a <= b ? a : b;

    public static TextPosition Max(TextPosition a, TextPosition b) => a >= b ? a : b;
}

internal static class CodeText
{
    private enum CharClass
    {
        Space,
        Word,
        Symbol,
    }

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

    public static (TextPosition Start, TextPosition End) LineAt(DecompiledDocument document, int line) =>
        line + 1 < document.Lines.Length
            ? (new TextPosition(line, 0), new TextPosition(line + 1, 0))
            : (new TextPosition(line, 0), new TextPosition(line, document.Lines[line].Text.Length));

    public static (TextPosition Start, TextPosition End) WordAt(DecompiledDocument document, TextPosition at)
    {
        var text = document.Lines[at.Line].Text;
        if (text.Length == 0) return (at, at);
        var probe = at.Column < text.Length ? at.Column : at.Column - 1;
        var kind = Classify(text[probe]);
        var start = probe;
        while (start > 0 && Classify(text[start - 1]) == kind) start--;
        var end = probe + 1;
        while (end < text.Length && Classify(text[end]) == kind) end++;
        return (new TextPosition(at.Line, start), new TextPosition(at.Line, end));
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

    private static CharClass Classify(char c) =>
        char.IsWhiteSpace(c) ? CharClass.Space
        : char.IsLetterOrDigit(c) || c == '_' || char.IsSurrogate(c) ? CharClass.Word
        : CharClass.Symbol;
}
