using ZGF.Gui;

namespace ZGF.Gui.Tests;

// FakeCanvas bills every UTF-16 code unit at 8px, so "a😀😀" (5 code units) measures 40px and the
// ellipsis 8px. At 28px the widest prefix the pixel budget allows is 2 code units — the middle of
// the first emoji. Cutting there orphans a high surrogate, which renders as tofu.
public class TextEllipsisTests
{
    private static readonly TextStyle Style = new();
    private static readonly ICanvas Canvas = new FakeCanvas();

    private static string Truncate(string text, float available) =>
        TextEllipsis.Truncate(Canvas, text, Style, available);

    [Fact]
    public void NeverCutsInsideASurrogatePair()
    {
        var result = Truncate("a😀😀", 28f);

        Assert.DoesNotContain(result, char.IsSurrogate);
        Assert.Equal("a…", result);
    }

    [Fact]
    public void NeverCutsInsideAnEmojiCluster()
    {
        // 👍🏽 is a base emoji plus a skin-tone modifier — one cluster, four code units.
        var result = Truncate("ab👍🏽", 40f);

        Assert.Equal("ab…", result);
    }

    [Fact]
    public void TrimsAsciiToTheBudget()
    {
        Assert.Equal("abcd…", Truncate("abcdefgh", 40f));
    }

    [Fact]
    public void TextThatFitsIsReturnedUnchanged()
    {
        Assert.Equal("abc", Truncate("abc", 100f));
    }

    [Fact]
    public void WidthTooSmallForTheEllipsisYieldsABareEllipsis()
    {
        Assert.Equal("…", Truncate("abcdef", 4f));
    }
}
