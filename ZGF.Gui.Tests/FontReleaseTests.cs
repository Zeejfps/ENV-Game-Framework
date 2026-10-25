using ZGF.Fonts;

namespace ZGF.Gui.Tests;

public class FontReleaseTests
{
    private const int PixelSize = 16;
    private const int LargerPixelSize = 24;
    private const string Sample = "Released";

    private static string InterPath => Path.Combine(AppContext.BaseDirectory, "Assets", "Inter-Regular.ttf");

    private static void Rasterize(FreeTypeFontBackend fonts, FontHandle font)
    {
        foreach (var c in Sample)
            fonts.TryGetGlyph(fonts.ResolveGlyph(font, c), out _);
    }

    [Fact]
    public void A_variant_cannot_be_released_alone_and_asking_changes_nothing()
    {
        using var fonts = new FreeTypeFontBackend();
        var font = fonts.LoadFontFromFile(InterPath, PixelSize);
        var larger = fonts.GetSizedVariant(font, LargerPixelSize);
        var bold = fonts.GetEmboldenedVariant(font);
        Rasterize(fonts, larger);
        var glyphs = fonts.CachedGlyphCount;

        Assert.Throws<ArgumentException>(() => fonts.ReleaseFont(larger));
        Assert.Throws<ArgumentException>(() => fonts.ReleaseFont(bold));

        Assert.Equal(3, fonts.LoadedFontCount);
        Assert.Equal(glyphs, fonts.CachedGlyphCount);
        Assert.Equal(larger, fonts.GetSizedVariant(font, LargerPixelSize));
        fonts.ReleaseFont(font);
        Assert.Equal(0, fonts.LoadedFontCount);
    }

    [Fact]
    public void Releasing_a_font_releases_its_variants_and_its_glyphs_and_its_handle_is_refused()
    {
        using var fonts = new FreeTypeFontBackend();
        var font = fonts.LoadFontFromFile(InterPath, PixelSize);
        var larger = fonts.GetSizedVariant(font, LargerPixelSize);
        var bold = fonts.GetEmboldenedVariant(larger);
        Rasterize(fonts, font);
        Rasterize(fonts, bold);

        fonts.ReleaseFont(font);

        Assert.Equal(0, fonts.LoadedFontCount);
        Assert.Equal(0, fonts.CachedGlyphCount);
        Assert.Throws<ArgumentException>(() => fonts.GetMetrics(font));
        Assert.Throws<ArgumentException>(() => fonts.GetMetrics(bold));
    }

    [Fact]
    public void Glyphs_of_a_font_loaded_after_a_release_take_the_space_its_glyphs_gave_back()
    {
        using var fonts = new FreeTypeFontBackend();
        var first = fonts.LoadFontFromFile(InterPath, PixelSize);
        Rasterize(fonts, first);
        fonts.ReleaseFont(first);
        AdvanceFrames(fonts, GlyphAtlas.QuarantineFrames);
        var freed = fonts.FreedAtlasSpaceCount;

        var second = fonts.LoadFontFromFile(InterPath, PixelSize);
        Rasterize(fonts, second);

        Assert.True(freed > 0);
        Assert.True(fonts.FreedAtlasSpaceCount < freed);
        Assert.True(fonts.TryGetGlyph(fonts.ResolveGlyph(second, 'R'), out var glyph));
        Assert.True(glyph.Width > 0);
    }

    [Fact]
    public void A_released_glyph_keeps_its_pixels_and_space_until_no_frame_can_sample_it()
    {
        using var fonts = new FreeTypeFontBackend();
        var font = fonts.LoadFontFromFile(InterPath, PixelSize);
        Assert.True(fonts.TryGetGlyph(fonts.ResolveGlyph(font, 'R'), out var glyph));
        var ink = InkOf(fonts, glyph);

        fonts.ReleaseFont(font);
        AdvanceFrames(fonts, GlyphAtlas.QuarantineFrames - 1);
        var inkDuringQuarantine = InkOf(fonts, glyph);
        var freedDuringQuarantine = fonts.FreedAtlasSpaceCount;
        AdvanceFrames(fonts, 1);

        Assert.True(ink > 0);
        Assert.Equal(ink, inkDuringQuarantine);
        Assert.Equal(0, freedDuringQuarantine);
        Assert.Equal(0, InkOf(fonts, glyph));
        Assert.Equal(1, fonts.FreedAtlasSpaceCount);
        Assert.Equal(0, fonts.QuarantinedGlyphCount);
    }

    private static void AdvanceFrames(FreeTypeFontBackend fonts, int count)
    {
        for (var frame = 0; frame < count; frame++)
            fonts.AdvanceFrame();
    }

    /// <summary>The sum of the atlas pixels <paramref name="glyph"/> was drawn into.</summary>
    private static int InkOf(FreeTypeFontBackend fonts, GlyphRenderInfo glyph)
    {
        var pixels = fonts.AtlasPixels;
        var ink = 0;
        for (var row = 0; row < glyph.Height; row++)
        {
            for (var column = 0; column < glyph.Width; column++)
                ink += pixels[(glyph.AtlasY + row) * fonts.AtlasWidth + glyph.AtlasX + column];
        }
        return ink;
    }

    [Fact]
    public void A_font_still_loaded_keeps_its_glyphs_when_another_is_released()
    {
        using var fonts = new FreeTypeFontBackend();
        var kept = fonts.LoadFontFromFile(InterPath, PixelSize);
        var released = fonts.LoadFontFromFile(InterPath, LargerPixelSize);
        Rasterize(fonts, kept);
        var glyphs = fonts.CachedGlyphCount;
        Rasterize(fonts, released);

        fonts.ReleaseFont(released);

        Assert.Equal(glyphs, fonts.CachedGlyphCount);
        Assert.Equal(1, fonts.LoadedFontCount);
        Assert.True(fonts.TryGetGlyph(fonts.ResolveGlyph(kept, 'R'), out _));
    }
}
