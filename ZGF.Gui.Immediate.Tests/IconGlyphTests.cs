using ZGF.Gui.Immediate.Widgets;
using ZGF.Svg;

namespace ZGF.Gui.Immediate.Tests;

public sealed class IconGlyphTests
{
    private const uint Red = 0xFFFF0000;
    private const uint Blue = 0xFF0000FF;
    private const float Size = 16f;

    private static readonly IconGlyph Tinted = IconGlyph.FromSvg(SvgDocument.Parse(
        """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 10 10"><rect width="10" height="10" fill="currentColor"/></svg>"""));

    private static readonly IconGlyph Green = IconGlyph.FromSvg(SvgDocument.Parse(
        """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 10 10"><rect width="10" height="10" fill="#00FF00"/></svg>"""));

    [Fact]
    public void A_font_glyph_is_drawn_as_text_in_its_family()
    {
        var frames = new UiFrames(new DefaultUiState(), body: ui => ui.Glyph(IconGlyph.FromFont("A", "Icons"), Red, Size)).Frame();

        var text = Assert.Single(frames.Canvas.Texts);
        Assert.Equal("A", text.Inputs.Text);
        Assert.Equal("Icons", text.Inputs.Style.FontFamily.Value);
        Assert.Equal(Red, text.Inputs.Style.TextColor.Value);
        Assert.Empty(frames.Canvas.Images);
    }

    [Fact]
    public void A_picture_is_drawn_square_at_its_size_in_the_color_asked_for_through_currentColor()
    {
        var frames = new UiFrames(new DefaultUiState(), body: ui => ui.Glyph(Tinted, Red, Size)).Frame();

        var drawn = Assert.Single(frames.Canvas.Images);
        var pixels = frames.Canvas.PixelsOf(drawn.Inputs.ImageId);
        Assert.Equal((Size, Size), (drawn.Inputs.Position.Width, drawn.Inputs.Position.Height));
        Assert.Equal(16 * 16 * 4, pixels.Length);
        Assert.Equal([0xFF, 0x00, 0x00, 0xFF], CenterPixel(pixels, 16));
        Assert.Empty(frames.Canvas.Texts);
    }

    [Fact]
    public void A_picture_without_currentColor_keeps_its_own_colors_and_one_image_for_every_color()
    {
        var frames = new UiFrames(new DefaultUiState(), body: ui =>
        {
            ui.Glyph(Green, Red, Size);
            ui.Glyph(Green, Blue, Size);
        }).Frame();

        var ids = frames.Canvas.Images
            .Select(i => i.Inputs.ImageId)
            .Distinct()
            .ToList();
        var id = Assert.Single(ids);
        Assert.Equal([0x00, 0xFF, 0x00, 0xFF], CenterPixel(frames.Canvas.PixelsOf(id), 16));
    }

    [Fact]
    public void Each_color_and_pixel_size_of_a_tinted_picture_is_an_image_of_its_own_rasterized_once()
    {
        var frames = new UiFrames(new DefaultUiState(), body: ui =>
        {
            ui.Glyph(Tinted, Red, Size);
            ui.Glyph(Tinted, Blue, Size);
            ui.Glyph(Tinted, Red, Size * 2);
        });

        var rasters = frames.Frame(10).Frame(static ui => ui.IconRasterCount);

        Assert.Equal(3, rasters);
        Assert.Equal(3, frames.Canvas.ImageIds.Count);
    }

    [Fact]
    public void A_picture_is_rasterized_at_the_size_it_is_drawn_in_pixels()
    {
        var frames = new UiFrames(new DefaultUiState(), body: ui => ui.Glyph(Tinted, Red, Size));
        frames.Canvas.DpiScale = 1.5f;

        frames.Frame();

        var id = Assert.Single(frames.Canvas.ImageIds);
        Assert.Equal(24 * 24 * 4, frames.Canvas.PixelsOf(id).Length);
        Assert.Equal(Size, frames.Canvas.Images[0].Inputs.Position.Width);
    }

    [Fact]
    public void Releasing_after_the_icons_change_frees_the_images_the_frame_did_not_draw()
    {
        var shown = Tinted;
        var frames = new UiFrames(new DefaultUiState(), body: ui => ui.Glyph(shown, Red, Size)).Frame();
        var old = Assert.Single(frames.Canvas.ImageIds);

        shown = Green;
        frames.Frame(static ui => ui.ReleaseUndrawnIconImages());

        var kept = Assert.Single(frames.Canvas.ImageIds);
        Assert.NotEqual(old, kept);
        Assert.Equal(kept, frames.Canvas.Images[0].Inputs.ImageId);
    }

    [Fact]
    public void An_image_not_drawn_for_a_while_is_freed()
    {
        var drawn = true;
        var frames = new UiFrames(new DefaultUiState(), body: ui =>
        {
            if (drawn)
                ui.Glyph(Tinted, Red, Size);
        }).Frame();
        drawn = false;

        frames.Frame(100);
        Assert.Single(frames.Canvas.ImageIds);
        frames.Frame(1000);

        Assert.Empty(frames.Canvas.ImageIds);
    }

    [Fact]
    public void An_icon_and_its_overlay_can_be_pictures()
    {
        var icon = new UiIcon(Tinted, Red, Overlay: Green);
        var frames = new UiFrames(new DefaultUiState(), body: ui => ui.Icon(icon)).Frame();

        Assert.Equal(2, frames.Canvas.Images.Count);
    }

    [Fact]
    public void A_painter_draws_a_picture_centered_in_the_rect()
    {
        var frames = new UiFrames(new DefaultUiState(), body: static ui =>
        {
            using (ui.Box(new Box { Width = 40f, Height = 40f }))
                ui.Paint(static p => p.Glyph(p.Rect, Tinted, Red, Size));
        }).Frame();

        var drawn = Assert.Single(frames.Canvas.Images).Inputs.Position;
        Assert.Equal((12f, 16f), (drawn.Left, drawn.Width));
    }

    [Fact]
    public void A_frame_of_pictures_drawn_before_allocates_nothing()
    {
        var frames = new UiFrames(new DefaultUiState(), body: static ui =>
        {
            ui.Glyph(Tinted, Red, Size);
            ui.Icon(new UiIcon(Tinted, Blue, Overlay: Green));
        }).Frame(4);
        frames.Canvas.Recording = false;

        var before = GC.GetAllocatedBytesForCurrentThread();
        frames.Frame(50);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(0, allocated);
    }

    private static byte[] CenterPixel(byte[] pixels, int side)
    {
        var at = (side / 2 * side + side / 2) * 4;
        return pixels.AsSpan(at, 4).ToArray();
    }
}

public sealed class IconSetTests
{
    private static readonly IconGlyph A = IconGlyph.FromFont("a");
    private static readonly IconGlyph B = IconGlyph.FromFont("b");

    [Fact]
    public void A_key_the_set_lacks_is_its_fallbacks_and_one_neither_has_draws_nothing()
    {
        var fallback = new IconSet(new Dictionary<string, IconGlyph> { ["one"] = A, ["two"] = A });
        var set = new IconSet(new Dictionary<string, IconGlyph> { ["one"] = B }, fallback);

        Assert.Equal(B, set.Find("one"));
        Assert.Equal(A, set.Find("two"));
        Assert.Equal(default, set.Find("three"));
        Assert.True(set.Contains("one"));
        Assert.False(set.Contains("two"));
    }
}
