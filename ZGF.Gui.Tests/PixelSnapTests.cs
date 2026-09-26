using ZGF.Fonts;
using ZGF.Geometry;
using ZGF.Gui.Testing;

namespace ZGF.Gui.Tests;

/// <summary>
/// What the canvas draws lands on the device pixel grid, not the logical point grid. The two only
/// coincide at whole-number scales; at 125% or 150% a whole point sits a fraction into a pixel, so a
/// point-snapped 1pt border covers one pixel in one place and two in the next.
/// </summary>
public sealed class PixelSnapTests : IDisposable
{
    private const int CanvasWidth = 400;
    private const int CanvasHeight = 120;
    private const uint Red = 0xFFFF0000u;
    private const uint Blue = 0xFF0000FFu;

    private static string FontPath => Path.Combine(AppContext.BaseDirectory, "Assets", "Inter-Regular.ttf");

    private readonly FreeTypeFontBackend _fonts = new();

    public void Dispose() => _fonts.Dispose();

    private RasterCanvas RasterAt(float dpiScale)
    {
        var font = _fonts.LoadFontFromFile(FontPath, (int)MathF.Round(16f * dpiScale));
        return new RasterCanvas(CanvasWidth, CanvasHeight, _fonts, font, dpiScale, 0u);
    }

    private CaptureCanvas CaptureAt(float dpiScale)
    {
        var font = _fonts.LoadFontFromFile(FontPath, (int)MathF.Round(16f * dpiScale));
        return new CaptureCanvas(_fonts, font, dpiScale);
    }

    private static void Fill(ICanvas canvas, RectF position, uint color, float leftBorder = 0f) =>
        canvas.DrawRect(new DrawRectInputs
        {
            Position = position,
            Style = new RectStyle
            {
                BackgroundColor = color,
                BorderSize = new BorderSizeStyle { Left = leftBorder },
                BorderColor = BorderColorStyle.All(Red),
            },
            ZIndex = 0,
        });

    private static byte[] Render(RasterCanvas canvas, Action<ICanvas> draw)
    {
        canvas.BeginFrame();
        draw(canvas);
        canvas.EndFrame();
        return canvas.ToRgbaTopDown();
    }

    // Counts the pixels of one colour along a device row, counted from the bottom like the canvas.
    private static int CountInRow(byte[] rgba, float dpiScale, int deviceRowFromBottom, int fromX, int toX, uint argb)
    {
        var width = (int)MathF.Round(CanvasWidth * dpiScale);
        var height = (int)MathF.Round(CanvasHeight * dpiScale);
        var row = height - 1 - deviceRowFromBottom;
        var count = 0;
        for (var x = fromX; x < toX; x++)
        {
            var i = (row * width + x) * 4;
            if (rgba[i] == (byte)(argb >> 16) && rgba[i + 1] == (byte)(argb >> 8) && rgba[i + 2] == (byte)argb
                && rgba[i + 3] == 0xFF)
                count++;
        }
        return count;
    }

    private static int CountAll(byte[] rgba, uint argb)
    {
        var count = 0;
        for (var i = 0; i < rgba.Length; i += 4)
            if (rgba[i] == (byte)(argb >> 16) && rgba[i + 1] == (byte)(argb >> 8) && rgba[i + 2] == (byte)argb
                && rgba[i + 3] == 0xFF)
                count++;
        return count;
    }

    private static readonly float[] Offsets = [0f, 0.25f, 0.4f, 0.5f, 0.6f, 0.75f, 1f, 1.3f];

    [Theory]
    [InlineData(1.25f)]
    [InlineData(1.5f)]
    [InlineData(1.75f)]
    public void RectEdgesLandOnDevicePixels(float dpiScale)
    {
        var canvas = CaptureAt(dpiScale);
        canvas.Frame(c =>
        {
            foreach (var offset in Offsets)
                Fill(c, new RectF(10f + offset, 20f + offset, 33.3f, 17.7f), Blue);
        });

        foreach (var rect in canvas.Rects)
        {
            foreach (var edge in new[] { rect.Rect.X, rect.Rect.Y, rect.Rect.X + rect.Rect.Z, rect.Rect.Y + rect.Rect.W })
            {
                var device = edge * dpiScale;
                Assert.Equal(MathF.Round(device), device, 0.01f);
            }
        }
    }

    [Theory]
    [InlineData(1f, 1)]
    [InlineData(1.25f, 1)]
    [InlineData(1.5f, 2)]
    [InlineData(1.75f, 2)]
    [InlineData(2f, 2)]
    public void AOnePointBorderIsTheSameWidthWhereverItSits(float dpiScale, int expectedPixels)
    {
        var canvas = RasterAt(dpiScale);
        var rgba = Render(canvas, c =>
        {
            for (var i = 0; i < Offsets.Length; i++)
                Fill(c, new RectF(10f + i * 40f + Offsets[i], 20f, 20f, 20f), Blue, leftBorder: 1f);
        });

        var row = (int)(30f * dpiScale);
        for (var i = 0; i < Offsets.Length; i++)
        {
            var from = (int)((10f + i * 40f - 2f) * dpiScale);
            var to = (int)((10f + i * 40f + 30f) * dpiScale);
            Assert.Equal(expectedPixels, CountInRow(rgba, dpiScale, row, from, to, Red));
        }
    }

    [Theory]
    [InlineData(1f, 1)]
    [InlineData(1.25f, 1)]
    [InlineData(1.5f, 2)]
    [InlineData(1.75f, 2)]
    public void AOnePointLineIsTheSameWidthWhereverItSits(float dpiScale, int expectedPixels)
    {
        var canvas = RasterAt(dpiScale);
        var rgba = Render(canvas, c =>
        {
            for (var i = 0; i < Offsets.Length; i++)
                Fill(c, new RectF(10f + i * 40f + Offsets[i], 20f, 1f, 20f), Red);
        });

        var row = (int)(30f * dpiScale);
        for (var i = 0; i < Offsets.Length; i++)
        {
            var from = (int)((10f + i * 40f - 2f) * dpiScale);
            var to = (int)((10f + i * 40f + 30f) * dpiScale);
            Assert.Equal(expectedPixels, CountInRow(rgba, dpiScale, row, from, to, Red));
        }
    }

    // Layout arithmetic leaves one neighbour's right edge a few ulps off the next one's left. At
    // 125% and 150% whole points are exact half pixels, where that noise used to round two ways.
    [Theory]
    [InlineData(1.25f, 10f)]
    [InlineData(1.5f, 11f)]
    [InlineData(1.25f, 30.4f)]
    [InlineData(1f, 20.5f)]
    public void NeighboursShareAnEdgeThroughFloatNoise(float dpiScale, float edge)
    {
        var canvas = CaptureAt(dpiScale);
        var justBelow = MathF.BitDecrement(MathF.BitDecrement(edge));
        var justAbove = MathF.BitIncrement(MathF.BitIncrement(edge));
        canvas.Frame(c =>
        {
            Fill(c, new RectF(0f, 0f, justBelow, 10f), Blue);
            Fill(c, new RectF(justAbove, 0f, 60f - justAbove, 10f), Red);
        });

        var left = canvas.Rects.Single(r => r.BgColor == Blue);
        var right = canvas.Rects.Single(r => r.BgColor == Red);
        Assert.Equal(left.Rect.X + left.Rect.Z, right.Rect.X, 0.0001f);
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(1.25f)]
    [InlineData(1.5f)]
    public void ClippingAViewToItsOwnBoundsKeepsAllOfItsBackground(float dpiScale)
    {
        var bounds = new RectF(10.3f, 10.6f, 40.4f, 20.3f);

        var unclipped = CountAll(Render(RasterAt(dpiScale), c => Fill(c, bounds, Blue)), Blue);
        var clipped = CountAll(Render(RasterAt(dpiScale), c =>
        {
            c.PushClip(bounds);
            Fill(c, bounds, Blue);
            c.PopClip();
        }), Blue);

        Assert.True(unclipped > 0);
        Assert.Equal(unclipped, clipped);
    }

    [Theory]
    [InlineData(1.25f)]
    [InlineData(1.5f)]
    public void TheCanvasFillsEveryDevicePixel(float dpiScale)
    {
        var canvas = RasterAt(dpiScale);
        var rgba = Render(canvas, c => Fill(c, new RectF(0f, 0f, CanvasWidth, CanvasHeight), Blue));

        Assert.Equal(rgba.Length / 4, CountAll(rgba, Blue));
    }
}
