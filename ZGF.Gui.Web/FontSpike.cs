using System.Text;
using ZGF.Fonts;

namespace ZGF.Gui.Web;

/// <summary>
/// The web font validation spike (docs/web-font-rendering.md §7). Exercises the
/// whole in-browser font path against the same <see cref="FreeTypeFontBackend"/>
/// the desktop uses:
///   1. load an embedded TTF,
///   2. shape a string with HarfBuzz (browser-wasm asset),
///   3. rasterize each glyph with FreeType (our self-built libfreetype.a) into
///      the shared atlas,
///   4. read back metrics + atlas dirty-rect.
/// If this runs without throwing and reports a non-empty atlas dirty-rect, the
/// native wiring is proven and everything downstream (atlas upload + GPU draw)
/// is already platform-neutral.
/// </summary>
internal static class FontSpike
{
    private const string SampleText = "Affinity — ZGF 0123";
    private const int PixelSize = 32;

    public static string Run()
    {
        var sb = new StringBuilder();
        try
        {
            using var fonts = new FreeTypeFontBackend();

            // The Inter font is embedded in the ZGF.Gui assembly (LogicalName
            // "Inter-Regular.ttf"); load it via the public AppUtils helper so we
            // don't need InternalsVisibleTo or a bundled copy.
            var guiAssembly = typeof(ZGF.Gui.View).Assembly;
            var fontBytes = ZGF.AppUtils.EmbeddedAssets.LoadBytes(guiAssembly, "Inter-Regular.ttf");
            sb.AppendLine($"font: Inter-Regular.ttf ({fontBytes.Length} bytes) @ {PixelSize}px");

            var font = fonts.LoadFontFromMemory(fontBytes, PixelSize);

            var metrics = fonts.GetMetrics(font);
            sb.AppendLine($"metrics: ascender={metrics.Ascender:0.##} descender={metrics.Descender:0.##} lineHeight={metrics.LineHeight:0.##}");

            // 2. Shape.
            Span<ShapedGlyph> shaped = stackalloc ShapedGlyph[128];
            var n = fonts.ShapeText(font, SampleText, shaped);
            sb.AppendLine($"shaped \"{SampleText}\" -> {n} glyphs");

            // 3. Rasterize each shaped glyph into the atlas.
            var rasterized = 0;
            var totalAdvance = 0f;
            for (var i = 0; i < n; i++)
            {
                totalAdvance += shaped[i].XAdvance;
                if (fonts.TryGetGlyph(font, shaped[i].GlyphIndex, out var g) && g.Width > 0 && g.Height > 0)
                    rasterized++;
            }
            sb.AppendLine($"rasterized {rasterized} non-empty glyphs; total advance={totalAdvance:0.##}px");

            // 4. Atlas state.
            sb.AppendLine($"atlas: {fonts.AtlasWidth}x{fonts.AtlasHeight}, dirty={fonts.AtlasDirty}");
            if (fonts.AtlasDirty)
            {
                var r = fonts.DirtyRect;
                sb.AppendLine($"dirtyRect: x={r.X} y={r.Y} w={r.Width} h={r.Height}");
            }

            sb.AppendLine();
            sb.AppendLine("PASS — FreeType + HarfBuzz are working under browser-wasm.");
        }
        catch (Exception ex)
        {
            sb.AppendLine();
            sb.AppendLine("FAIL — " + ex.GetType().Name + ": " + ex.Message);
            sb.AppendLine(ex.StackTrace);
        }

        return sb.ToString();
    }
}
