using ZGF.Gui;

namespace ZGF.Gui.Tests;

// FakeCanvas measures every UTF-16 code unit at 8px (space included), so widths in these
// cases are predictable: a width of N means N/8 code units fit on a line.
public class TextWrapperTests
{
    private static readonly TextStyle Style = new();

    private static List<string> Wrap(string text, float maxWidth)
    {
        var output = new List<string>();
        TextWrapper.Wrap(new FakeCanvas(), text, Style, maxWidth, output);
        return output;
    }

    [Fact]
    public void EmptyTextProducesOneEmptyLine()
    {
        Assert.Equal(new[] { "" }, Wrap("", 80f));
    }

    [Fact]
    public void TextThatFitsIsReturnedUnchanged()
    {
        Assert.Equal(new[] { "hello world" }, Wrap("hello world", 800f));
    }

    [Fact]
    public void NonPositiveWidthIsLeftUnwrapped()
    {
        Assert.Equal(new[] { "hello world" }, Wrap("hello world", 0f));
    }

    [Fact]
    public void NewlinesStartNewVisualLines()
    {
        Assert.Equal(new[] { "a", "b", "c" }, Wrap("a\nb\r\nc", 80f));
    }

    [Fact]
    public void LatinWrapsOnSpacesNotMidWord()
    {
        Assert.Equal(new[] { "aa bb", "cc" }, Wrap("aa bb cc", 40f));
    }

    [Fact]
    public void LatinWordWiderThanLineBreaksBetweenCharacters()
    {
        // No break opportunity inside the word, so it splits by code point rather than overflowing.
        Assert.Equal(new[] { "aa", "aa", "a" }, Wrap("aaaaa", 16f));
    }

    [Fact]
    public void OverWideWordStartsOnAFreshLine()
    {
        Assert.Equal(new[] { "aa", "bb", "bb" }, Wrap("aa bbbb", 16f));
    }

    [Fact]
    public void PathBreaksAfterSeparatorsNotMidSegment()
    {
        Assert.Equal(new[] { "usr/", "bin/", "sh" }, Wrap("usr/bin/sh", 32f));
    }

    [Fact]
    public void BackslashPathBreaksAfterSeparators()
    {
        Assert.Equal(new[] { "C:\\", "src\\", "a" }, Wrap("C:\\src\\a", 32f));
    }

    [Fact]
    public void HyphenAndUnderscoreAreBreakOpportunities()
    {
        Assert.Equal(new[] { "a-", "b_", "c" }, Wrap("a-b_c", 16f));
    }

    [Fact]
    public void SegmentTooLongForALineStillBreaksByCharacter()
    {
        // "aaaa" has no internal break opportunity and exceeds the line on its own.
        Assert.Equal(new[] { "x/", "aa", "aa" }, Wrap("x/aaaa", 16f));
    }

    [Fact]
    public void SeparatorDoesNotStartALine()
    {
        // '.' and ':' are kinsoku no-break-before, so they stay attached to the preceding segment.
        Assert.Equal(new[] { "ab.", "cd" }, Wrap("ab.cd", 24f));
    }

    [Fact]
    public void CjkBreaksBetweenIdeographs()
    {
        Assert.Equal(new[] { "世界", "平和" }, Wrap("世界平和", 16f));
    }

    [Fact]
    public void CjkThatFitsIsNotBroken()
    {
        Assert.Equal(new[] { "世界" }, Wrap("世界", 80f));
    }

    [Fact]
    public void MixedLatinAndCjkBreaksAtTheScriptBoundary()
    {
        Assert.Equal(new[] { "Hi", "世界" }, Wrap("Hi世界", 16f));
    }

    [Fact]
    public void ClosingPunctuationDoesNotStartALine()
    {
        // 。 must stay attached to the preceding ideograph (kinsoku: no break before).
        Assert.Equal(new[] { "あ。", "あ" }, Wrap("あ。あ", 8f));
    }

    [Fact]
    public void OpeningPunctuationDoesNotEndALine()
    {
        // 「 must stay attached to the following ideograph (kinsoku: no break after).
        Assert.Equal(new[] { "あ", "「い" }, Wrap("あ「い", 16f));
    }

    [Fact]
    public void SupplementaryCjkIsNeverSplitMidSurrogatePair()
    {
        const string a = "\U00020000"; // CJK Ext-B ideograph, 2 UTF-16 code units
        var lines = Wrap(a + a, 16f);
        Assert.Equal(new[] { a, a }, lines);
        Assert.All(lines, l => Assert.Equal(2, l.Length));
    }
}
