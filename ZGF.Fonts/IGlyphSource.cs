namespace ZGF.Fonts;

/// <summary>
/// Font-source abstraction consumed by the platform-neutral canvas
/// (<c>RenderedCanvasBase</c>): text shaping, glyph rasterization into a shared
/// atlas, font metrics, and sized/emboldened variants.
///
/// <para><see cref="FreeTypeFontBackend"/> is the default implementation
/// (HarfBuzz shaping + FreeType rasterization). This seam exists so an alternate
/// rasterizer — e.g. a managed backend, or the same FreeType/HarfBuzz stack
/// linked as <c>browser-wasm</c> static libs — can be substituted without
/// touching the canvas. See <c>docs/web-font-rendering.md</c>.</para>
///
/// <para>The glyph atlas itself (<see cref="ZGF.Fonts"/>'s skyline packer) is
/// platform-neutral and shared by every implementation; the atlas members below
/// expose the R8 pixel buffer and dirty-rect the render backend uploads to the
/// GPU.</para>
/// </summary>
public interface IGlyphSource
{
    // ---- Shaping ----

    /// Shapes <paramref name="text"/> into <paramref name="output"/> with no
    /// OpenType features, returning the number of shaped glyphs written.
    int ShapeText(FontHandle font, ReadOnlySpan<char> text, Span<ShapedGlyph> output);

    /// Shapes <paramref name="text"/> applying <paramref name="features"/>,
    /// returning the number of shaped glyphs written to <paramref name="output"/>.
    int ShapeText(FontHandle font, ReadOnlySpan<char> text, Span<ShapedGlyph> output, in FontFeatureSet features);

    // ---- Rasterization / metrics ----

    /// Rasterizes the glyph into the shared atlas (if not already cached) and
    /// returns its bitmap placement/metrics. False if the glyph can't be produced.
    bool TryGetGlyph(FontHandle font, uint glyphIndex, out GlyphRenderInfo info);

    /// Ascender/descender/line-height for the font, in device pixels.
    FontMetrics GetMetrics(FontHandle font);

    // ---- Variants ----

    /// Returns a handle to the same font at <paramref name="pixelSize"/>.
    FontHandle GetSizedVariant(FontHandle baseFont, int pixelSize);

    /// Returns a sibling handle that renders the font with synthesized bold.
    FontHandle GetEmboldenedVariant(FontHandle baseFont);

    // ---- Shared glyph atlas (uploaded by the render backend) ----

    int AtlasWidth { get; }
    int AtlasHeight { get; }
    ReadOnlySpan<byte> AtlasPixels { get; }
    bool AtlasDirty { get; }
    AtlasDirtyRect DirtyRect { get; }
    void ClearDirty();
}
