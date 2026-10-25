using ZGF.Svg;

namespace ZGF.Gui.Immediate;

public abstract partial class Ui
{
    private readonly IconImages _iconImages = new();

    /// <summary>Records <paramref name="glyph"/> at <paramref name="size"/>, the theme's font size when
    /// null, in <paramref name="color"/>. A font glyph is a text element, aligned by
    /// <paramref name="align"/>; a picture is rasterized at the size it is drawn in pixels with
    /// <paramref name="color"/> as its <c>currentColor</c>, and drawn as a square of
    /// <paramref name="size"/> centered in an element that <paramref name="box"/> sizes, or that is that
    /// square when it does not.</summary>
    public void Glyph(IconGlyph glyph, uint color, float? size = null, Box box = default, Align align = Align.Center)
    {
        if (glyph.Svg != null)
        {
            SvgGlyph(glyph.Svg, color, size ?? Theme.FontSize, box);
            return;
        }
        if (glyph.Text != null)
            Text(glyph.Text, box, color, size, align: align, family: glyph.Family);
    }

    private void SvgGlyph(SvgDocument svg, uint color, float size, Box box)
    {
        var imageId = AcquireIconImage(svg, size, color);
        using (Box(box with { Width = box.Width ?? size, Height = box.Height ?? size }))
            Paint((imageId, size), static (painter, s) => painter.Image(UiPainter.Centered(painter.Rect, s.size), s.imageId));
    }

    /// <summary>The image of <paramref name="svg"/> drawn <paramref name="size"/> square in
    /// <paramref name="color"/> at the canvas's scale, rasterized the first time.</summary>
    internal string AcquireIconImage(SvgDocument svg, float size, uint color)
    {
        var pixels = Math.Max(1, (int)MathF.Ceiling(size * _canvas.DpiScale));
        return _iconImages.Acquire(_canvas, svg, pixels, color, _frame);
    }

    /// <summary>Frees, once this frame is drawn, the images of the SVG icons it did not draw, as when
    /// the icons changed. Images not drawn for a while are freed anyway.</summary>
    public void ReleaseUndrawnIconImages() => _iconImages.ReleaseUndrawn();

    /// <summary>How many times an SVG icon was rasterized, for tests to tell a reuse from a redraw.</summary>
    internal int IconRasterCount => _iconImages.RasterCount;

    private void EndIconImages() => _iconImages.EndFrame(_canvas, _frame);
}
