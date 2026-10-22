using System.Globalization;
using System.Text;

namespace ZGF.Gui;

/// <summary>
/// Grapheme-cluster and word boundaries over a UTF-16 buffer. A UTF-16 index is not a character
/// position: an emoji or an astral ideograph is a surrogate pair, and an emoji with a skin-tone
/// modifier is two runes that read as one. Caret movement, deletion, truncation and any index
/// derived from a click go through here so no slice lands inside a cluster.
/// </summary>
public static class TextBoundaries
{
    /// <summary>The cluster boundary at or before <paramref name="index"/> — <paramref name="index"/>
    /// itself when it is already one.</summary>
    public static int Snap(ReadOnlySpan<char> text, int index)
    {
        if (index <= 0) return 0;
        if (index >= text.Length) return text.Length;

        // Clusters never span a '\n' (UAX-29 breaks on both sides of it), so enumerating from the
        // start of the caret's line is exact and costs a line rather than the whole buffer.
        var i = LineStart(text, index);
        while (i < index)
        {
            var next = i + StringInfo.GetNextTextElementLength(text[i..]);
            if (next > index)
                return i;
            i = next;
        }
        return index;
    }

    /// <summary>The first cluster boundary after <paramref name="index"/>.</summary>
    public static int Next(ReadOnlySpan<char> text, int index)
    {
        if (index >= text.Length) return text.Length;
        var start = Snap(text, index);
        return Math.Min(start + StringInfo.GetNextTextElementLength(text[start..]), text.Length);
    }

    /// <summary>The last cluster boundary before <paramref name="index"/>.</summary>
    public static int Prev(ReadOnlySpan<char> text, int index)
    {
        if (index <= 0) return 0;
        return Snap(text, Math.Min(index, text.Length) - 1);
    }

    /// <summary>
    /// The start of the next word, as Ctrl+Right (and forward word-delete) should land: past the
    /// current run of word characters and the separators after it. An ideograph is its own word —
    /// CJK has no spaces, so a letter-run scan would otherwise swallow a whole sentence. With
    /// <paramref name="splitPunctuation"/>, a run of punctuation is a word of its own and only
    /// whitespace separates.
    /// </summary>
    public static int NextWord(ReadOnlySpan<char> text, int index, bool splitPunctuation = false)
    {
        var i = Math.Clamp(index, 0, text.Length);
        if (i >= text.Length) return text.Length;

        var cls = ClassAt(text, i, splitPunctuation);
        if (cls == CharClass.Ideographic)
            return NextRune(text, i);

        if (cls != CharClass.Separator)
            i = SkipForward(text, i, cls, splitPunctuation);
        return SkipForward(text, i, CharClass.Separator, splitPunctuation);
    }

    /// <summary>The start of the word before <paramref name="index"/> — where Ctrl+Left and
    /// Ctrl+Backspace stop. <paramref name="splitPunctuation"/> is as in <see cref="NextWord"/>.</summary>
    public static int PrevWord(ReadOnlySpan<char> text, int index, bool splitPunctuation = false)
    {
        var i = Math.Clamp(index, 0, text.Length);
        if (i <= 0) return 0;

        if (ClassBefore(text, i, splitPunctuation) == CharClass.Ideographic)
            return PrevRune(text, i);

        i = SkipBackward(text, i, CharClass.Separator, splitPunctuation);
        if (i <= 0) return 0;
        var cls = ClassBefore(text, i, splitPunctuation);
        if (cls == CharClass.Ideographic) return i;
        return SkipBackward(text, i, cls, splitPunctuation);
    }

    /// <summary>
    /// The word (or same-class run) around <paramref name="index"/> — the span a double-click
    /// selects. Unlike <see cref="PrevWord"/>/<see cref="NextWord"/>, which pair a word with its
    /// trailing separators for caret navigation, this expands over one contiguous run of the same
    /// class (letters/digits, or separators, or with <paramref name="splitPunctuation"/> whitespace
    /// and punctuation apart) so the selection is just the thing under the cursor. An ideograph is
    /// its own word, matching CJK caret navigation. An empty buffer yields an empty span at 0.
    /// </summary>
    public static void WordAt(ReadOnlySpan<char> text, int index, out int start, out int end, bool splitPunctuation = false)
    {
        index = Math.Clamp(index, 0, text.Length);
        if (text.Length == 0)
        {
            start = 0;
            end = 0;
            return;
        }

        // The run's class is the one under the caret (the character to its right); at the very end
        // of the buffer there is nothing to the right, so fall back to the character before it.
        var atEnd = index >= text.Length;
        var cls = atEnd ? ClassBefore(text, index, splitPunctuation) : ClassAt(text, index, splitPunctuation);

        // An ideograph stands alone — select the single cluster the caret sits on.
        if (cls == CharClass.Ideographic)
        {
            end = atEnd ? index : NextRune(text, index);
            start = PrevRune(text, end);
            return;
        }

        start = SkipBackward(text, index, cls, splitPunctuation);
        end = SkipForward(text, index, cls, splitPunctuation);
    }

    private enum CharClass
    {
        Word,
        Ideographic,
        Separator,
        Punctuation,
    }

    private static int SkipForward(ReadOnlySpan<char> text, int index, CharClass cls, bool splitPunctuation)
    {
        while (index < text.Length && ClassAt(text, index, splitPunctuation) == cls)
            index = NextRune(text, index);
        return index;
    }

    private static int SkipBackward(ReadOnlySpan<char> text, int index, CharClass cls, bool splitPunctuation)
    {
        while (index > 0 && ClassBefore(text, index, splitPunctuation) == cls)
            index = PrevRune(text, index);
        return index;
    }

    private static CharClass ClassAt(ReadOnlySpan<char> text, int index, bool splitPunctuation)
    {
        Rune.DecodeFromUtf16(text[index..], out var rune, out _);
        return Classify(rune, splitPunctuation);
    }

    private static CharClass ClassBefore(ReadOnlySpan<char> text, int index, bool splitPunctuation)
    {
        Rune.DecodeLastFromUtf16(text[..index], out var rune, out _);
        return Classify(rune, splitPunctuation);
    }

    private static CharClass Classify(Rune rune, bool splitPunctuation)
    {
        if (TextWrapper.IsWide(rune.Value))
            return CharClass.Ideographic;
        if (Rune.IsLetterOrDigit(rune) || rune.Value == '_')
            return CharClass.Word;
        if (splitPunctuation && !Rune.IsWhiteSpace(rune))
            return CharClass.Punctuation;
        return CharClass.Separator;
    }

    private static int NextRune(ReadOnlySpan<char> text, int index)
    {
        Rune.DecodeFromUtf16(text[index..], out _, out var consumed);
        return Math.Min(index + consumed, text.Length);
    }

    private static int PrevRune(ReadOnlySpan<char> text, int index)
    {
        Rune.DecodeLastFromUtf16(text[..index], out _, out var consumed);
        return Math.Max(index - consumed, 0);
    }

    private static int LineStart(ReadOnlySpan<char> text, int index) =>
        text[..index].LastIndexOf('\n') + 1;
}
