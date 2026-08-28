using ZGF.Fonts;

namespace ZGF.Gui.Tests;

/// <summary>
/// <see cref="FreeTypeFontBackend.ResolveGlyph"/>: the shaping-free code point lookup the cell path
/// runs once per cell. It caches, so the test that matters is the one where the cached answer has
/// to be thrown away.
/// </summary>
public class GlyphResolutionTests
{
    // U+4E00, the CJK ideograph for "one". Inter does not cover it; a CJK system font does.
    private const int Han = 0x4E00;

    private static string InterPath => Path.Combine(AppContext.BaseDirectory, "Assets", "Inter-Regular.ttf");

    private static string? CjkFontPath()
    {
        var candidates = OperatingSystem.IsWindows()
            ? new[]
            {
                Path.Combine(WinFonts, "YuGothR.ttc"),
                Path.Combine(WinFonts, "msgothic.ttc"),
                Path.Combine(WinFonts, "simsun.ttc"),
                Path.Combine(WinFonts, "malgun.ttf"),
            }
            : OperatingSystem.IsMacOS()
                ? new[]
                {
                    "/System/Library/Fonts/Hiragino Sans GB.ttc",
                    "/System/Library/Fonts/PingFang.ttc",
                }
                : new[]
                {
                    "/usr/share/fonts/opentype/noto/NotoSansCJK-Regular.ttc",
                    "/usr/share/fonts/truetype/droid/DroidSansFallbackFull.ttf",
                };

        return candidates.FirstOrDefault(File.Exists);
    }

    private static string WinFonts => Environment.GetFolderPath(Environment.SpecialFolder.Fonts);

    [Fact]
    public void ACodePointThePrimaryCovers_ComesFromThePrimary()
    {
        using var fonts = new FreeTypeFontBackend();
        var inter = fonts.LoadFontFromFile(InterPath, 16);

        var resolved = fonts.ResolveGlyph(inter, 'A');

        Assert.Equal(inter, resolved.Font);
        Assert.False(resolved.IsMissing);
    }

    [Fact]
    public void ACodePointNothingCovers_ResolvesToThePrimarysNotdef()
    {
        using var fonts = new FreeTypeFontBackend();
        var inter = fonts.LoadFontFromFile(InterPath, 16);

        var resolved = fonts.ResolveGlyph(inter, Han);

        Assert.Equal(inter, resolved.Font);
        Assert.True(resolved.IsMissing);
    }

    [Fact]
    public void RegisteringAFallback_InvalidatesACodePointAlreadyResolvedAsMissing()
    {
        var cjk = CjkFontPath();
        if (cjk is null) return; // no CJK font on this machine

        using var fonts = new FreeTypeFontBackend();
        var inter = fonts.LoadFontFromFile(InterPath, 16);

        // Ask before the fallback exists, so the miss is in the cache when it registers. Without the
        // invalidation this stays tofu for the life of the process.
        Assert.True(fonts.ResolveGlyph(inter, Han).IsMissing);

        fonts.RegisterFallbackFont(fonts.LoadFontFromFile(cjk, 16));

        var resolved = fonts.ResolveGlyph(inter, Han);
        Assert.False(resolved.IsMissing);
        Assert.NotEqual(inter, resolved.Font);
    }

    [Fact]
    public void AFallbackGlyph_IsResolvedAtThePrimarysPixelSize()
    {
        var cjk = CjkFontPath();
        if (cjk is null) return; // no CJK font on this machine

        using var fonts = new FreeTypeFontBackend();
        var small = fonts.LoadFontFromFile(InterPath, 16);
        var large = fonts.LoadFontFromFile(InterPath, 32);
        fonts.RegisterFallbackFont(fonts.LoadFontFromFile(cjk, 16));

        // The fallback was loaded at one size and is being substituted into runs at two. A glyph
        // that came back at the size the fallback happened to be loaded at would render at the
        // wrong height beside its neighbours in one of them.
        Assert.True(fonts.TryGetGlyph(Resolve(small), out var smallGlyph));
        Assert.True(fonts.TryGetGlyph(Resolve(large), out var largeGlyph));
        Assert.True(largeGlyph.XAdvance > smallGlyph.XAdvance,
            $"substituted glyph advances {largeGlyph.XAdvance} at 32px and {smallGlyph.XAdvance} at 16px");

        GlyphRef Resolve(FontHandle primary) => fonts.ResolveGlyph(primary, Han);
    }

    [Fact]
    public void RepeatedResolution_AnswersTheSameGlyph()
    {
        using var fonts = new FreeTypeFontBackend();
        var inter = fonts.LoadFontFromFile(InterPath, 16);

        Assert.Equal(fonts.ResolveGlyph(inter, 'A'), fonts.ResolveGlyph(inter, 'A'));
    }
}
