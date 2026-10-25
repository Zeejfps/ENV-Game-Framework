using ZGF.AppUtils;
using ZGF.Fonts;
using ZGF.Geometry;
using ZGF.Gui.Testing;

namespace ZGF.Gui.Immediate.Tests;

public sealed class FontRegistryTests : IDisposable
{
    private const string Family = "Mine";
    private const string Other = "Theirs";
    private const string Sample = "Sample text 0123";
    private const int PixelSize = 16;
    private const int Saves = 5;

    private static readonly byte[] Regular = Load("Inter-Regular.ttf");
    private static readonly byte[] SemiBold = Load("Inter-SemiBold.ttf");

    private readonly FreeTypeFontBackend _fonts = new();
    private readonly RasterCanvas _canvas;
    private readonly FontRegistry _registry;
    private readonly TextStyle _style = new() { FontFamily = Family, FontSize = 16f };

    public FontRegistryTests()
    {
        var defaultFont = _fonts.LoadFontFromMemory(Regular, PixelSize);
        _canvas = new RasterCanvas(200, 50, _fonts, defaultFont);
        _registry = new FontRegistry(_fonts, _canvas, LoadAtPixelSize);
    }

    public void Dispose() => _fonts.Dispose();

    private static byte[] Load(string file) => EmbeddedAssets.LoadBytes(typeof(ICanvas).Assembly, file);

    private FontHandle LoadAtPixelSize(byte[] data, int faceIndex) => _fonts.LoadFontFromMemory(data, PixelSize, faceIndex);

    /// <summary><see cref="Regular"/> with <paramref name="extra"/> bytes after it: a file saved again,
    /// its content changed, that still loads as the same font.</summary>
    private static byte[] Saved(int extra) => Regular.Concat(new byte[extra]).ToArray();

    private void DrawFrame()
    {
        _canvas.BeginFrame();
        DrawSample();
        _canvas.EndFrame();
    }

    private void DrawSample()
    {
        _canvas.DrawText(new DrawTextInputs { Position = new RectF(0f, 0f, 200f, 50f), Text = Sample, Style = _style, ZIndex = 0 });
    }

    [Fact]
    public void Registering_unchanged_bytes_again_from_new_arrays_keeps_one_face()
    {
        _registry.Register(new FontFace(Family, Regular.ToArray()));
        DrawFrame();
        var loaded = _fonts.LoadedFontCount;

        for (var reload = 0; reload < Saves; reload++)
        {
            _registry.Register(new FontFace(Family, Regular.ToArray()));
            _registry.ReleaseUnused();
            DrawFrame();
        }

        Assert.Equal(1, _registry.LoadedCount);
        Assert.Equal(loaded, _fonts.LoadedFontCount);
    }

    [Fact]
    public void Registering_changed_bytes_again_and_again_for_one_family_leaves_one_live_face_and_its_glyphs()
    {
        _registry.Register(new FontFace(Family, Saved(0)));
        DrawFrame();
        var loaded = _fonts.LoadedFontCount;
        var glyphs = _fonts.CachedGlyphCount;

        for (var save = 1; save <= Saves; save++)
        {
            _registry.Register(new FontFace(Family, Saved(save)));
            _registry.ReleaseUnused();
            DrawFrame();
        }

        Assert.Equal(1, _registry.LoadedCount);
        Assert.Equal(loaded, _fonts.LoadedFontCount);
        Assert.Equal(glyphs, _fonts.CachedGlyphCount);
    }

    [Fact]
    public void A_replaced_faces_glyphs_leave_the_atlas_and_the_new_faces_take_their_space()
    {
        _registry.Register(new FontFace(Family, Saved(0)));
        DrawFrame();
        var glyphs = _fonts.CachedGlyphCount;

        _registry.Register(new FontFace(Family, Saved(1)));
        _registry.ReleaseUnused();
        var quarantined = _fonts.QuarantinedGlyphCount;
        var freedAtOnce = _fonts.FreedAtlasSpaceCount;
        for (var frame = 0; frame < GlyphAtlas.QuarantineFrames; frame++)
            _fonts.AdvanceFrame();
        var freed = _fonts.FreedAtlasSpaceCount;
        var left = _fonts.CachedGlyphCount;
        DrawFrame();

        Assert.True(left < glyphs, $"{left} glyphs left of {glyphs}");
        Assert.True(quarantined > 0);
        Assert.Equal(0, freedAtOnce);
        Assert.True(freed > 0);
        Assert.True(_fonts.FreedAtlasSpaceCount < freed, $"{_fonts.FreedAtlasSpaceCount} freed spaces left of {freed}");
    }

    [Fact]
    public void Text_in_a_family_registered_again_is_drawn_with_the_new_face()
    {
        _registry.Register(new FontFace(Family, Regular));
        DrawFrame();
        var regular = _canvas.MeasureTextWidth(Sample, _style);

        _registry.Register(new FontFace(Family, SemiBold));
        _registry.ReleaseUnused();
        DrawFrame();

        Assert.True(_canvas.MeasureTextWidth(Sample, _style) > regular);
    }

    [Fact]
    public void A_face_replaced_mid_frame_keeps_its_glyphs_until_the_frame_is_over()
    {
        _registry.Register(new FontFace(Family, Saved(0)));
        DrawFrame();
        var glyphs = _fonts.CachedGlyphCount;

        _canvas.BeginFrame();
        DrawSample();
        _registry.Register(new FontFace(Family, Saved(1)));
        DrawSample();
        _canvas.EndFrame();
        var kept = _fonts.CachedGlyphCount;
        _registry.ReleaseUnused();

        Assert.True(kept > glyphs, $"{kept} glyphs cached during the frame, {glyphs} before it");
        Assert.Equal(glyphs, _fonts.CachedGlyphCount);
    }

    [Fact]
    public void An_unregistered_family_lets_go_of_its_face_unless_another_family_draws_with_it()
    {
        _registry.Register(new FontFace(Family, Regular));
        _registry.Register(new FontFace(Other, Regular.ToArray()));
        var loaded = _fonts.LoadedFontCount;

        _registry.Unregister(Family);
        _registry.ReleaseUnused();
        Assert.Equal(loaded, _fonts.LoadedFontCount);

        _registry.Unregister(Other);
        _registry.ReleaseUnused();
        Assert.Equal(loaded - 1, _fonts.LoadedFontCount);
        Assert.Equal(0, _registry.LoadedCount);
        DrawFrame();
    }
}
