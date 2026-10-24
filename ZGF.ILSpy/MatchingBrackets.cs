using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;

namespace ZGF.ILSpy;

/// <summary>Where a bracket and the one matching it are in a document.</summary>
public readonly record struct BracketPair(TextPosition Open, TextPosition Close);

/// <summary>The bracket at a code view's caret, or just before it, and the one that matches it,
/// looked up again only when the caret or the document changes. Brackets in strings and comments
/// do not count, and the match is looked for only so many lines away.</summary>
public sealed class MatchingBrackets
{
    private const string Opens = "([{";
    private const string Closes = ")]}";
    private const int MaxLinesScanned = 2000;

    private DecompiledDocument? _document;
    private TextPosition _caret;
    private BracketPair? _found;

    public BracketPair? Find(DecompiledDocument document, TextPosition caret)
    {
        if (IsLookedUp(document, caret))
            return _found;
        _document = document;
        _caret = caret;
        _found = Match(document, caret);
        return _found;
    }

    private bool IsLookedUp(DecompiledDocument document, TextPosition caret)
    {
        if (!ReferenceEquals(document, _document))
            return false;
        return caret == _caret;
    }

    private static BracketPair? Match(DecompiledDocument document, TextPosition caret)
    {
        var after = MatchAt(document, caret);
        if (after != null)
            return after;
        if (caret.Column == 0)
            return null;
        var before = caret with { Column = caret.Column - 1 };
        return MatchAt(document, before);
    }

    private static BracketPair? MatchAt(DecompiledDocument document, TextPosition at)
    {
        var bracket = BracketAt(document, at);
        var open = Opens.IndexOf(bracket);
        if (open >= 0)
            return MatchForward(document, at, Opens[open], Closes[open]);
        var close = Closes.IndexOf(bracket);
        if (close >= 0)
            return MatchBackward(document, at, Opens[close], Closes[close]);
        return null;
    }

    /// <summary>The character at <paramref name="at"/> when it is code, not in a string or comment; else none.</summary>
    private static char BracketAt(DecompiledDocument document, TextPosition at)
    {
        if (at.Line < 0 || at.Line >= document.Lines.Length)
            return '\0';
        var line = document.Lines[at.Line];
        var span = CodeText.SpanAt(line.Spans, at.Column);
        if (span < 0)
            return '\0';
        if (!IsCode(line.Spans[span]))
            return '\0';
        return line.Text[at.Column];
    }

    private static bool IsCode(DocumentSpan span)
    {
        if (span.Kind == SpanKind.String)
            return false;
        return span.Kind != SpanKind.Comment;
    }

    private static BracketPair? MatchForward(DecompiledDocument document, TextPosition open, char opening, char closing)
    {
        var depth = 0;
        var last = Math.Min(document.Lines.Length - 1, open.Line + MaxLinesScanned);
        for (var index = open.Line; index <= last; index++)
        {
            var line = document.Lines[index];
            var from = index == open.Line ? open.Column + 1 : 0;
            foreach (var span in line.Spans)
            {
                if (span.End <= from)
                    continue;
                if (!IsCode(span))
                    continue;
                for (var column = Math.Max(span.Start, from); column < span.End; column++)
                {
                    var c = line.Text[column];
                    if (c == opening)
                        depth++;
                    if (c != closing)
                        continue;
                    if (depth == 0)
                        return new BracketPair(open, new TextPosition(index, column));
                    depth--;
                }
            }
        }
        return null;
    }

    private static BracketPair? MatchBackward(DecompiledDocument document, TextPosition close, char opening, char closing)
    {
        var depth = 0;
        var first = Math.Max(0, close.Line - MaxLinesScanned);
        for (var index = close.Line; index >= first; index--)
        {
            var line = document.Lines[index];
            var before = index == close.Line ? close.Column : line.Text.Length;
            var spans = line.Spans;
            for (var s = spans.Length - 1; s >= 0; s--)
            {
                var span = spans[s];
                if (span.Start >= before)
                    continue;
                if (!IsCode(span))
                    continue;
                for (var column = Math.Min(span.End, before) - 1; column >= span.Start; column--)
                {
                    var c = line.Text[column];
                    if (c == closing)
                        depth++;
                    if (c != opening)
                        continue;
                    if (depth == 0)
                        return new BracketPair(new TextPosition(index, column), close);
                    depth--;
                }
            }
        }
        return null;
    }
}
