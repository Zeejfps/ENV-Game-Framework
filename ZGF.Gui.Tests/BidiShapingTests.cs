using ZGF.Fonts;

namespace ZGF.Gui.Tests;

// Exercises the BiDi shaping path with real fonts: the bundled Inter (Latin, no Arabic coverage)
// as primary plus an OS Arabic font as fallback. Skips when no Arabic font is present.
public class BidiShapingTests
{
    private static string InterPath => Path.Combine(AppContext.BaseDirectory, "Assets", "Inter-Regular.ttf");

    private static string? ArabicFontPath()
    {
        var candidates = OperatingSystem.IsWindows()
            ? new[]
            {
                Path.Combine(WinFonts, "arial.ttf"),
                Path.Combine(WinFonts, "tahoma.ttf"),
                Path.Combine(WinFonts, "segoeui.ttf"),
            }
            : OperatingSystem.IsMacOS()
                ? new[]
                {
                    "/System/Library/Fonts/Supplemental/GeezaPro.ttc",
                    "/Library/Fonts/GeezaPro.ttc",
                    "/System/Library/Fonts/Supplemental/Arial.ttf",
                }
                : new[]
                {
                    "/usr/share/fonts/truetype/noto/NotoNaskhArabic-Regular.ttf",
                    "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
                    "/usr/share/fonts/dejavu/DejaVuSans.ttf",
                };

        foreach (var c in candidates)
            if (File.Exists(c))
                return c;
        return null;
    }

    private static string WinFonts => Environment.GetFolderPath(Environment.SpecialFolder.Fonts);

    [Fact]
    public void PureArabic_RendersFromFallback_InVisualOrder()
    {
        var arabic = ArabicFontPath();
        if (arabic is null) return; // no Arabic font on this machine

        var fonts = new FreeTypeFontBackend();
        try
        {
            var primary = fonts.LoadFontFromFile(InterPath, 16);
            var fallback = fonts.LoadFontFromFile(arabic, 16);
            fonts.RegisterFallbackFont(fallback);

            const string text = "مرحبا"; // مرحبا
            Span<ShapedGlyph> glyphs = stackalloc ShapedGlyph[32];
            var n = fonts.ShapeText(primary, text, glyphs);

            Assert.True(n >= 2);
            for (var i = 0; i < n; i++)
            {
                Assert.Equal(fallback.Id, glyphs[i].FontId);  // covered by the Arabic fallback
                Assert.NotEqual(0u, glyphs[i].GlyphIndex);    // not .notdef
            }

            // RTL: visual left-to-right output corresponds to non-increasing logical clusters.
            for (var i = 1; i < n; i++)
                Assert.True(glyphs[i].Cluster <= glyphs[i - 1].Cluster,
                    "RTL clusters should not increase left-to-right");
        }
        finally { fonts.Dispose(); }
    }

    [Fact]
    public void MixedLatinArabic_SplitsAcrossFonts_AndOrdersVisually()
    {
        var arabic = ArabicFontPath();
        if (arabic is null) return;

        var fonts = new FreeTypeFontBackend();
        try
        {
            var primary = fonts.LoadFontFromFile(InterPath, 16);
            var fallback = fonts.LoadFontFromFile(arabic, 16);
            fonts.RegisterFallbackFont(fallback);

            // Base auto-detects LTR (first strong is 'a'); the Arabic tail is an RTL island.
            const string text = "abمر"; // a b م ر
            Span<ShapedGlyph> glyphs = stackalloc ShapedGlyph[32];
            var n = fonts.ShapeText(primary, text, glyphs);

            Assert.Equal(4, n);
            Assert.Equal(primary.Id, glyphs[0].FontId);
            Assert.Equal(primary.Id, glyphs[1].FontId);
            Assert.Equal(0, glyphs[0].Cluster); // 'a' first (LTR)
            Assert.Equal(1, glyphs[1].Cluster); // 'b'
            Assert.Equal(fallback.Id, glyphs[2].FontId);
            Assert.Equal(fallback.Id, glyphs[3].FontId);
            // The Arabic island is internally reversed: its visually-left glyph is the
            // logically-later character (cluster 3 before cluster 2).
            Assert.True(glyphs[2].Cluster > glyphs[3].Cluster);
        }
        finally { fonts.Dispose(); }
    }

    [Fact]
    public void ArabicClusterPrefixAdvances_AreMonotonic_AndMarkIsZeroWidth()
    {
        // The caret model sums the advances of fully-shaped glyphs whose cluster precedes the caret
        // (in-context), instead of re-measuring a detached prefix. This must be monotonic so the caret
        // never moves backwards, and a zero-width combining mark must not shift it.
        var arabic = ArabicFontPath();
        if (arabic is null) return;

        var fonts = new FreeTypeFontBackend();
        try
        {
            var primary = fonts.LoadFontFromFile(InterPath, 16);
            var fallback = fonts.LoadFontFromFile(arabic, 16);
            fonts.RegisterFallbackFont(fallback);

            const string text = "مرحبًا"; // م ر ح ب ً ا — index 4 is the tanwin (U+064B), a zero-width mark
            Span<ShapedGlyph> glyphs = stackalloc ShapedGlyph[64];
            var n = fonts.ShapeText(primary, text, glyphs);

            var snapshot = glyphs[..n].ToArray();
            float Prefix(int k)
            {
                var sum = 0f;
                foreach (var g in snapshot)
                    if (g.Cluster < k) sum += g.XAdvance;
                return sum;
            }

            var prev = 0f;
            for (var k = 0; k <= text.Length; k++)
            {
                var p = Prefix(k);
                Assert.True(p >= prev - 0.01f, $"prefix({k})={p} regressed below {prev}");
                prev = p;
            }

            // The combining mark contributes no advance, so the caret x is unchanged across it.
            Assert.Equal(Prefix(4), Prefix(5), 3);
            // The whole word still has real width.
            Assert.True(Prefix(text.Length) > 0f);
        }
        finally { fonts.Dispose(); }
    }

    [Fact]
    public void ExplicitBaseDirection_FlipsRunOrderOfMixedLine()
    {
        var arabic = ArabicFontPath();
        if (arabic is null) return;

        var fonts = new FreeTypeFontBackend();
        try
        {
            var primary = fonts.LoadFontFromFile(InterPath, 16);
            var fallback = fonts.LoadFontFromFile(arabic, 16);
            fonts.RegisterFallbackFont(fallback);

            // "ab" (LTR) + "مر" (RTL island). The canvas threads the UI base direction here so an
            // ambiguous line follows the locale instead of the first-strong heuristic.
            const string text = "abمر";
            Span<ShapedGlyph> ltr = stackalloc ShapedGlyph[32];
            Span<ShapedGlyph> rtl = stackalloc ShapedGlyph[32];
            var nLtr = fonts.ShapeText(primary, text, ltr, FontFeatureSet.None, BidiDirection.Ltr);
            var nRtl = fonts.ShapeText(primary, text, rtl, FontFeatureSet.None, BidiDirection.Rtl);

            // LTR base: the Latin run is visually first (leftmost glyph is 'a' from the primary).
            Assert.Equal(primary.Id, ltr[0].FontId);
            Assert.Equal(0, ltr[0].Cluster);

            // RTL base: the logically-first Latin run moves to the right, so the leftmost glyph now
            // comes from the Arabic island (the fallback font).
            Assert.Equal(fallback.Id, rtl[0].FontId);
            Assert.Equal(nLtr, nRtl);
        }
        finally { fonts.Dispose(); }
    }

    [Fact]
    public void ArabicWithLatinPrimaryNoFallback_StaysNotdef_ButReordered()
    {
        var fonts = new FreeTypeFontBackend();
        try
        {
            var primary = fonts.LoadFontFromFile(InterPath, 16);

            const string text = "مر"; // م ر, no fallback registered
            Span<ShapedGlyph> glyphs = stackalloc ShapedGlyph[16];
            var n = fonts.ShapeText(primary, text, glyphs);

            Assert.True(n >= 1);
            // Without an Arabic fallback the glyphs are .notdef, but the BiDi path still ran:
            // every glyph comes from the primary and the run is RTL (clusters non-increasing).
            for (var i = 0; i < n; i++) Assert.Equal(primary.Id, glyphs[i].FontId);
            for (var i = 1; i < n; i++) Assert.True(glyphs[i].Cluster <= glyphs[i - 1].Cluster);
        }
        finally { fonts.Dispose(); }
    }
}
