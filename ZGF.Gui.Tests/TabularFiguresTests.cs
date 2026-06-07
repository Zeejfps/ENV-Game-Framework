using ZGF.Fonts;

namespace ZGF.Gui.Tests;

public class TabularFiguresTests
{
    private const float Tolerance = 0.01f;

    private static string InterPath =>
        Path.Combine(AppContext.BaseDirectory, "Assets", "Inter-Regular.ttf");

    private static float ShapedAdvance(FreeTypeFontBackend fonts, FontHandle font, string text, in FontFeatureSet features)
    {
        Span<ShapedGlyph> shaped = stackalloc ShapedGlyph[64];
        var n = fonts.ShapeText(font, text, shaped, features);
        var total = 0f;
        for (var i = 0; i < n; i++)
            total += shaped[i].XAdvance;
        return total;
    }

    [Fact]
    public void TabularFigures_EqualLengthDigitStringsMeasureEqual()
    {
        var fonts = new FreeTypeFontBackend();
        try
        {
            var font = fonts.LoadFontFromFile(InterPath, 32);
            var tnum = FontFeatureSet.TabularFigures;

            var w111 = ShapedAdvance(fonts, font, "111", tnum);
            var w888 = ShapedAdvance(fonts, font, "888", tnum);
            var w109 = ShapedAdvance(fonts, font, "109", tnum);

            Assert.Equal(w111, w888, Tolerance);
            Assert.Equal(w111, w109, Tolerance);
        }
        finally
        {
            fonts.Dispose();
        }
    }

    [Fact]
    public void TabularFigures_EveryDigitSharesOneAdvance()
    {
        var fonts = new FreeTypeFontBackend();
        try
        {
            var font = fonts.LoadFontFromFile(InterPath, 32);

            Span<ShapedGlyph> shaped = stackalloc ShapedGlyph[16];
            var n = fonts.ShapeText(font, "1234567890", shaped, FontFeatureSet.TabularFigures);
            Assert.Equal(10, n);

            var first = shaped[0].XAdvance;
            for (var i = 1; i < n; i++)
                Assert.Equal(first, shaped[i].XAdvance, Tolerance);
        }
        finally
        {
            fonts.Dispose();
        }
    }

    [Fact]
    public void FeatureBuckets_DoNotCollideAndAreStable()
    {
        var fonts = new FreeTypeFontBackend();
        try
        {
            var font = fonts.LoadFontFromFile(InterPath, 32);
            var none = FontFeatureSet.None;
            var tnum = FontFeatureSet.TabularFigures;

            var none1 = ShapedAdvance(fonts, font, "123", none);
            var tnum1 = ShapedAdvance(fonts, font, "123", tnum);
            var none2 = ShapedAdvance(fonts, font, "123", none);
            var tnum2 = ShapedAdvance(fonts, font, "123", tnum);

            Assert.Equal(none1, none2, Tolerance);
            Assert.Equal(tnum1, tnum2, Tolerance);
        }
        finally
        {
            fonts.Dispose();
        }
    }

    [Fact]
    public void EmptyFeatureSet_HasZeroSignature_DistinctFromTabular()
    {
        Assert.True(FontFeatureSet.None.IsEmpty);
        Assert.Equal(0UL, FontFeatureSet.None.Signature);
        Assert.NotEqual(0UL, FontFeatureSet.TabularFigures.Signature);
        Assert.NotEqual(FontFeatureSet.None, FontFeatureSet.TabularFigures);
    }
}
