namespace ZGF.Gui.Tests;

public class TextBoundariesTests
{
    private const string Text = "foo.bar  baz";

    [Theory]
    [InlineData(1, 0, 3)]
    [InlineData(3, 3, 4)]
    [InlineData(8, 7, 9)]
    [InlineData(12, 9, 12)]
    public void SplitWordAtIsARunOfOneKindOfCharacter(int index, int start, int end)
    {
        TextBoundaries.WordAt(Text, index, out var foundStart, out var foundEnd, splitPunctuation: true);

        Assert.Equal((start, end), (foundStart, foundEnd));
    }

    [Theory]
    [InlineData(12, 9)]
    [InlineData(9, 4)]
    [InlineData(4, 3)]
    [InlineData(3, 0)]
    public void SplitPrevWordSkipsSpacesThenOneRun(int index, int expected)
    {
        Assert.Equal(expected, TextBoundaries.PrevWord(Text, index, splitPunctuation: true));
    }

    [Theory]
    [InlineData(0, 3)]
    [InlineData(4, 9)]
    [InlineData(7, 9)]
    [InlineData(9, 12)]
    public void SplitNextWordSkipsOneRunThenSpaces(int index, int expected)
    {
        Assert.Equal(expected, TextBoundaries.NextWord(Text, index, splitPunctuation: true));
    }

    [Fact]
    public void PunctuationSeparatesWordsUnlessSplit()
    {
        Assert.Equal(4, TextBoundaries.NextWord(Text, 0));
        Assert.Equal(0, TextBoundaries.PrevWord(Text, 4));
    }

    [Fact]
    public void WordAtJoinsPunctuationAndSpacesUnlessSplit()
    {
        TextBoundaries.WordAt("foo. bar", 3, out var start, out var end);
        TextBoundaries.WordAt("foo. bar", 3, out var splitStart, out var splitEnd, splitPunctuation: true);

        Assert.Equal((3, 5), (start, end));
        Assert.Equal((3, 4), (splitStart, splitEnd));
    }

    [Fact]
    public void SplitNextWordStepsOverAnIdeographAlone()
    {
        Assert.Equal(1, TextBoundaries.NextWord("中文 abc", 0, splitPunctuation: true));
    }

    [Fact]
    public void SplitNextWordStopsAtAnEmojiRunButNeverInsideIt()
    {
        Assert.Equal(2, TextBoundaries.NextWord("ab😀😀 cd", 0, splitPunctuation: true));
        Assert.Equal(7, TextBoundaries.NextWord("ab😀😀 cd", 2, splitPunctuation: true));
    }
}
