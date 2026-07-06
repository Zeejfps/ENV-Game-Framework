namespace ZGF.Svg.Tests;

/// <summary>
/// Platform-exact rasterizer checks against analytically known coverage — no golden files.
/// </summary>
public sealed class RasterizerAnalyticTests
{
    private static byte[] Raster(string svg, int w, int h, uint currentColor = 0xFF000000)
    {
        return SvgDocument.Parse(svg).Rasterize(w, h, currentColor);
    }

    private static byte A(byte[] rgba, int w, int x, int y) => rgba[(y * w + x) * 4 + 3];

    [Fact]
    public void FullCanvasRectIsExactColor()
    {
        var rgba = Raster("""<svg viewBox="0 0 8 8"><rect width="8" height="8" fill="#336699"/></svg>""", 8, 8);
        for (var i = 0; i < 8 * 8; i++)
        {
            Assert.Equal(0x33, rgba[i * 4]);
            Assert.Equal(0x66, rgba[i * 4 + 1]);
            Assert.Equal(0x99, rgba[i * 4 + 2]);
            Assert.Equal(0xFF, rgba[i * 4 + 3]);
        }
    }

    [Fact]
    public void HalfPixelCoverageIsHalfAlpha()
    {
        // Rect covers the left half of pixel column 4 exactly (x: 0..4.5 of 8).
        var rgba = Raster("""<svg viewBox="0 0 8 8"><rect width="4.5" height="8"/></svg>""", 8, 8);
        Assert.Equal(255, A(rgba, 8, 3, 4));
        Assert.Equal(127, A(rgba, 8, 4, 4), 1f);
        Assert.Equal(0, A(rgba, 8, 5, 4));
    }

    [Fact]
    public void CircleTotalCoverageMatchesArea()
    {
        const int size = 64;
        var rgba = Raster($"""<svg viewBox="0 0 {size} {size}"><circle cx="32" cy="32" r="20"/></svg>""", size, size);
        var total = 0.0;
        for (var i = 0; i < size * size; i++)
            total += rgba[i * 4 + 3];
        var expected = Math.PI * 20 * 20 * 255;
        Assert.InRange(total, expected * 0.995, expected * 1.005);
    }

    [Fact]
    public void EvenOddDonutHasTransparentHole()
    {
        var svg = """
            <svg viewBox="0 0 64 64">
              <path fill-rule="evenodd" d="M32 4 A28 28 0 1 1 31.99 4 Z M32 20 A12 12 0 1 1 31.99 20 Z"/>
            </svg>
            """;
        var rgba = Raster(svg, 64, 64);
        Assert.Equal(0, A(rgba, 64, 32, 32));      // hole center
        Assert.Equal(255, A(rgba, 64, 32, 10));    // ring
        Assert.Equal(0, A(rgba, 64, 1, 1));        // outside
    }

    [Fact]
    public void NonZeroSelfIntersectingStarIsSolid()
    {
        // Five-point star drawn edge-to-edge (self-intersecting) — nonzero keeps the core filled.
        var svg = """
            <svg viewBox="0 0 64 64">
              <path d="M32 4 L44 52 L6 22 L58 22 L20 52 Z"/>
            </svg>
            """;
        var rgba = Raster(svg, 64, 64);
        Assert.Equal(255, A(rgba, 64, 32, 28));  // pentagon core
    }

    [Fact]
    public void EvenOddStarHasHollowCore()
    {
        var svg = """
            <svg viewBox="0 0 64 64">
              <path fill-rule="evenodd" d="M32 4 L44 52 L6 22 L58 22 L20 52 Z"/>
            </svg>
            """;
        var rgba = Raster(svg, 64, 64);
        Assert.Equal(0, A(rgba, 64, 32, 28));
    }

    [Fact]
    public void StrokeCoversExpectedArea()
    {
        // Horizontal 4px-wide stroke across the full 32px canvas: area = 32 * 4.
        var svg = """<svg viewBox="0 0 32 32"><line x1="0" y1="16" x2="32" y2="16" stroke="black" stroke-width="4"/></svg>""";
        var rgba = Raster(svg, 32, 32);
        var total = 0.0;
        for (var i = 0; i < 32 * 32; i++)
            total += rgba[i * 4 + 3];
        var expected = 32 * 4 * 255.0;
        Assert.InRange(total, expected * 0.99, expected * 1.01);
        Assert.Equal(255, A(rgba, 32, 16, 15));
        Assert.Equal(255, A(rgba, 32, 16, 16));
        Assert.Equal(0, A(rgba, 32, 16, 20));
    }

    [Fact]
    public void StrokeScalesWithViewBoxTransform()
    {
        // Same as above but rendered at 2x: stroke should be 8 device px wide.
        var svg = """<svg viewBox="0 0 32 32"><line x1="0" y1="16" x2="32" y2="16" stroke="black" stroke-width="4"/></svg>""";
        var rgba = Raster(svg, 64, 64);
        var total = 0.0;
        for (var i = 0; i < 64 * 64; i++)
            total += rgba[i * 4 + 3];
        var expected = 64 * 8 * 255.0;
        Assert.InRange(total, expected * 0.99, expected * 1.01);
    }

    [Fact]
    public void AspectFitLetterboxesWithTransparency()
    {
        // 2:1 viewBox fully filled, rendered into a square: top and bottom quarters transparent.
        var rgba = Raster("""<svg viewBox="0 0 16 8"><rect width="16" height="8"/></svg>""", 16, 16);
        Assert.Equal(0, A(rgba, 16, 8, 1));
        Assert.Equal(255, A(rgba, 16, 8, 8));
        Assert.Equal(0, A(rgba, 16, 8, 14));
    }

    [Fact]
    public void CurrentColorUsesCallerColor()
    {
        var rgba = Raster("""<svg viewBox="0 0 4 4"><rect width="4" height="4" fill="currentColor"/></svg>""", 4, 4, 0xFF3366CC);
        Assert.Equal(0x33, rgba[0]);
        Assert.Equal(0x66, rgba[1]);
        Assert.Equal(0xCC, rgba[2]);
        Assert.Equal(0xFF, rgba[3]);
    }

    [Fact]
    public void SemiTransparentOverlapCompositesCorrectly()
    {
        // Two 50%-alpha black rects overlapping: overlap alpha = 1-(0.5)^2 = 0.75.
        var svg = """
            <svg viewBox="0 0 8 8">
              <rect width="6" height="8" fill-opacity="0.5"/>
              <rect x="2" width="6" height="8" fill-opacity="0.5"/>
            </svg>
            """;
        var rgba = Raster(svg, 8, 8);
        Assert.Equal(128, A(rgba, 8, 1, 4), 2f);
        Assert.Equal(191, A(rgba, 8, 4, 4), 2f);
    }

    [Fact]
    public void DashedLineHasGaps()
    {
        var svg = """<svg viewBox="0 0 32 8"><line x1="0" y1="4" x2="32" y2="4" stroke="black" stroke-width="2" stroke-dasharray="4 4"/></svg>""";
        var rgba = Raster(svg, 32, 8);
        Assert.Equal(255, A(rgba, 32, 2, 4));   // first dash on
        Assert.Equal(0, A(rgba, 32, 6, 4));     // first gap
        Assert.Equal(255, A(rgba, 32, 10, 4));  // second dash
    }

    [Fact]
    public void GeometryOutsideCanvasIsClippedSafely()
    {
        // Shape wildly larger than the canvas and partially negative — must not throw.
        var svg = """<svg viewBox="0 0 8 8"><rect x="-100" y="-100" width="1000" height="104" /></svg>""";
        var rgba = Raster(svg, 8, 8);
        Assert.Equal(255, A(rgba, 8, 4, 2));
    }

    [Fact]
    public void EmptyDocumentRasterizesToTransparent()
    {
        var rgba = Raster("""<svg viewBox="0 0 4 4"/>""", 4, 4);
        Assert.All(rgba, b => Assert.Equal(0, b));
    }

    [Fact]
    public void WrongBufferSizeThrows()
    {
        var doc = SvgDocument.Parse("""<svg viewBox="0 0 4 4"><rect width="4" height="4"/></svg>""");
        var rasterizer = new SvgRasterizer();
        Assert.Throws<ArgumentException>(() => rasterizer.Rasterize(doc, new byte[10], 4, 4));
    }

    [Fact]
    public void ReusedRasterizerDoesNotAllocateSteadyState()
    {
        var doc = SvgDocument.Parse("""
            <svg viewBox="0 0 24 24">
              <circle cx="12" cy="12" r="10" fill="none" stroke="currentColor" stroke-width="2" stroke-dasharray="3 2"/>
              <path d="M8 12 l3 3 l5 -6" fill="none" stroke="red" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
              <rect x="2" y="2" width="6" height="6" rx="1" fill-rule="evenodd"/>
            </svg>
            """);
        var rasterizer = new SvgRasterizer();
        var buffer = new byte[48 * 48 * 4];

        // Warm up: grows the pooled scratch buffers.
        rasterizer.Rasterize(doc, buffer, 48, 48, 0xFF123456);

        var before = GC.GetAllocatedBytesForCurrentThread();
        rasterizer.Rasterize(doc, buffer, 48, 48, 0xFF123456);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal(0, allocated);
    }
}
