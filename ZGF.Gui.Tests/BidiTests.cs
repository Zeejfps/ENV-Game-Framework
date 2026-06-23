using ZGF.Fonts;

namespace ZGF.Gui.Tests;

public class BidiTests
{
    private const string Arabic = "مرحبا"; // مرحبا (5 letters)
    private const string Hebrew = "שלום";       // שלום  (4 letters)

    [Fact]
    public void PureLatin_NotRtl_AllLevelZero()
    {
        Assert.False(Bidi.ContainsRtl("hello world 123"));
        var levels = Bidi.ResolveLevels("hello", BidiDirection.Auto, out var para);
        Assert.Equal(0, (int)para);
        Assert.All(levels, l => Assert.Equal(0, (int)l));
    }

    [Fact]
    public void PureArabic_AutoBase_IsRtl_AllLevelOne()
    {
        Assert.True(Bidi.ContainsRtl(Arabic));
        var levels = Bidi.ResolveLevels(Arabic, BidiDirection.Auto, out var para);
        Assert.Equal(1, (int)para);
        Assert.Equal(Arabic.Length, levels.Length);
        Assert.All(levels, l => Assert.Equal(1, (int)l));
    }

    [Fact]
    public void PureHebrew_AutoBase_IsRtl()
    {
        Assert.True(Bidi.ContainsRtl(Hebrew));
        var levels = Bidi.ResolveLevels(Hebrew, BidiDirection.Auto, out var para);
        Assert.Equal(1, (int)para);
        Assert.All(levels, l => Assert.Equal(1, (int)l));
    }

    [Fact]
    public void LatinEmbeddedInRtl_GetsEvenLevelTwo()
    {
        var text = Hebrew + " abc";
        var levels = Bidi.ResolveLevels(text, BidiDirection.Auto, out var para);
        Assert.Equal(1, (int)para);
        for (var i = 0; i < Hebrew.Length; i++) Assert.Equal(1, (int)levels[i]);
        Assert.Equal(1, (int)levels[Hebrew.Length]);     // medial space stays at base
        Assert.Equal(2, (int)levels[Hebrew.Length + 1]); // a
        Assert.Equal(2, (int)levels[Hebrew.Length + 2]); // b
        Assert.Equal(2, (int)levels[Hebrew.Length + 3]); // c
    }

    [Fact]
    public void NumberInRtl_FormsLtrIsland()
    {
        var text = Arabic + " 123 " + Arabic;
        var levels = Bidi.ResolveLevels(text, BidiDirection.Auto, out var para);
        Assert.Equal(1, (int)para);
        var digit = Arabic.Length + 1;
        Assert.Equal(2, (int)levels[digit]);     // 1
        Assert.Equal(2, (int)levels[digit + 1]); // 2
        Assert.Equal(2, (int)levels[digit + 2]); // 3
    }

    [Fact]
    public void RtlEmbeddedInLatin_IsLevelOne_BaseStaysLtr()
    {
        var text = "go " + Hebrew + " now";
        var levels = Bidi.ResolveLevels(text, BidiDirection.Auto, out var para);
        Assert.Equal(0, (int)para);
        Assert.Equal(0, (int)levels[0]); // g
        for (var i = 0; i < Hebrew.Length; i++) Assert.Equal(1, (int)levels[3 + i]);
    }

    [Fact]
    public void ExplicitBaseDirection_OverridesContent()
    {
        var ltr = Bidi.ResolveLevels(Arabic, BidiDirection.Ltr, out var p0);
        Assert.Equal(0, (int)p0);
        Assert.All(ltr, l => Assert.Equal(1, (int)l)); // R in an LTR base -> level 1

        var rtl = Bidi.ResolveLevels("abc", BidiDirection.Rtl, out var p1);
        Assert.Equal(1, (int)p1);
        Assert.All(rtl, l => Assert.Equal(2, (int)l)); // L in an RTL base -> level 2
    }

    [Fact]
    public void TrailingWhitespace_IsBaseLevel()
    {
        var text = "abc " + Hebrew + "  ";
        var levels = Bidi.ResolveLevels(text, BidiDirection.Auto, out var para);
        Assert.Equal(0, (int)para);
        Assert.Equal(0, (int)levels[^1]);
        Assert.Equal(0, (int)levels[^2]);
    }

    [Fact]
    public void SurrogatePair_SharesLevel()
    {
        // Hebrew letter + Mathematical Bold Capital A (U+1D400, a surrogate pair, strong L).
        var text = Hebrew[..1] + "𝐀";
        var levels = Bidi.ResolveLevels(text, BidiDirection.Auto, out var para);
        Assert.Equal(1, (int)para);          // first strong is the Hebrew letter (R)
        Assert.Equal(3, levels.Length);      // 1 Hebrew unit + 2 surrogate units
        Assert.Equal(1, (int)levels[0]);     // Hebrew
        Assert.Equal(2, (int)levels[1]);     // high surrogate (L island)
        Assert.Equal(2, (int)levels[2]);     // low surrogate shares the level
    }

    [Fact]
    public void OtherRtlScripts_ClassifyAsRtl()
    {
        // Beyond Arabic/Hebrew: Syriac (ܐܒ, AL), N'Ko (ߊߋ, R), Thaana (ހށ, AL).
        Assert.True(Bidi.ContainsRtl("ܐܒ"));
        Assert.True(Bidi.ContainsRtl("ߊߋ"));
        Assert.True(Bidi.ContainsRtl("ހށ"));

        var levels = Bidi.ResolveLevels("ܐܒ", BidiDirection.Auto, out var para);
        Assert.Equal(1, (int)para);
        Assert.All(levels, l => Assert.Equal(1, (int)l));
    }

    [Fact]
    public void VisualOrder_AllLtr_IsIdentity()
    {
        var order = new int[3];
        Bidi.ComputeVisualOrder(new byte[] { 0, 0, 0 }, order);
        Assert.Equal(new[] { 0, 1, 2 }, order);
    }

    [Fact]
    public void VisualOrder_AllRtl_IsReversed()
    {
        var order = new int[3];
        Bidi.ComputeVisualOrder(new byte[] { 1, 1, 1 }, order);
        Assert.Equal(new[] { 2, 1, 0 }, order);
    }

    [Fact]
    public void VisualOrder_LtrOrNumberIsland_InRtl_ReversesRunSequence()
    {
        // logical runs R(1), island(2), R(1): the run sequence reverses; the island block
        // keeps its internal (shaper-produced) order.
        var order = new int[3];
        Bidi.ComputeVisualOrder(new byte[] { 1, 2, 1 }, order);
        Assert.Equal(new[] { 2, 1, 0 }, order);
    }

    [Fact]
    public void VisualOrder_RtlIslandInLtr_KeepsRunOrder()
    {
        var order = new int[3];
        Bidi.ComputeVisualOrder(new byte[] { 0, 1, 0 }, order);
        Assert.Equal(new[] { 0, 1, 2 }, order);
    }
}
