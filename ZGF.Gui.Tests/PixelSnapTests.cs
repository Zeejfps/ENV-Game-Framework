using ZGF.Fonts;
using ZGF.Geometry;
using ZGF.Gui.Testing;
using ZGF.Gui.Testing.Gpu;

namespace ZGF.Gui.Tests;

/// <summary>
/// What the canvas draws lands on the device pixel grid, not the logical point grid. The two only
/// coincide at whole-number scales; at 125% or 150% a whole point sits a fraction into a pixel, so a
/// point-snapped 1pt border covers one pixel in one place and two in the next.
/// </summary>
[Collection(GpuCollection.Name)]
public sealed class PixelSnapTests(GpuDeviceFixture fixture) : IDisposable
{
    private const int CanvasWidth = 400;
    private const int CanvasHeight = 120;
    private const uint Red = 0xFFFF0000u;
    private const uint Blue = 0xFF0000FFu;

    private static string FontPath => Path.Combine(AppContext.BaseDirectory, "Assets", "Inter-Regular.ttf");

    private readonly FreeTypeFontBackend _fonts = new();

    public void Dispose() => _fonts.Dispose();

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

    /// <summary>What the shipping canvas draws, over nothing, at <paramref name="dpiScale"/>.</summary>
    private RgbaImage Render(float dpiScale, Action<ICanvas> draw)
    {
        using var session = fixture.Device.CreateSession(CanvasWidth, CanvasHeight, dpiScale);
        session.Draw(draw);
        return session.Read();
    }

    private static bool IsOpaque(Rgba8 pixel, uint argb) =>
        pixel == new Rgba8((byte)(argb >> 16), (byte)(argb >> 8), (byte)argb, 0xFF);

    // Counts the pixels of one colour along a device row, counted from the bottom like the canvas.
    private static int CountInRow(RgbaImage image, int deviceRowFromBottom, int fromX, int toX, uint argb)
    {
        var row = image.Height - 1 - deviceRowFromBottom;
        var count = 0;
        for (var x = fromX; x < toX; x++)
            if (IsOpaque(image[x, row], argb))
                count++;
        return count;
    }

    private static int CountAll(RgbaImage image, uint argb)
    {
        var count = 0;
        for (var y = 0; y < image.Height; y++)
        for (var x = 0; x < image.Width; x++)
            if (IsOpaque(image[x, y], argb))
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

    [GpuTheory]
    [InlineData(1f, 1)]
    [InlineData(1.25f, 1)]
    [InlineData(1.5f, 2)]
    [InlineData(1.75f, 2)]
    [InlineData(2f, 2)]
    public void AOnePointBorderIsTheSameWidthWhereverItSits(float dpiScale, int expectedPixels)
    {
        var image = Render(dpiScale, c =>
        {
            for (var i = 0; i < Offsets.Length; i++)
                Fill(c, new RectF(10f + i * 40f + Offsets[i], 20f, 20f, 20f), Blue, leftBorder: 1f);
        });

        var row = (int)(30f * dpiScale);
        for (var i = 0; i < Offsets.Length; i++)
        {
            var from = (int)((10f + i * 40f - 2f) * dpiScale);
            var to = (int)((10f + i * 40f + 30f) * dpiScale);
            Assert.Equal(expectedPixels, CountInRow(image, row, from, to, Red));
        }
    }

    [GpuTheory]
    [InlineData(1f, 1)]
    [InlineData(1.25f, 1)]
    [InlineData(1.5f, 2)]
    [InlineData(1.75f, 2)]
    public void AOnePointLineIsTheSameWidthWhereverItSits(float dpiScale, int expectedPixels)
    {
        var image = Render(dpiScale, c =>
        {
            for (var i = 0; i < Offsets.Length; i++)
                Fill(c, new RectF(10f + i * 40f + Offsets[i], 20f, 1f, 20f), Red);
        });

        var row = (int)(30f * dpiScale);
        for (var i = 0; i < Offsets.Length; i++)
        {
            var from = (int)((10f + i * 40f - 2f) * dpiScale);
            var to = (int)((10f + i * 40f + 30f) * dpiScale);
            Assert.Equal(expectedPixels, CountInRow(image, row, from, to, Red));
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

    [GpuTheory]
    [InlineData(1f)]
    [InlineData(1.25f)]
    [InlineData(1.5f)]
    public void ClippingAViewToItsOwnBoundsKeepsAllOfItsBackground(float dpiScale)
    {
        var bounds = new RectF(10.3f, 10.6f, 40.4f, 20.3f);

        var unclipped = CountAll(Render(dpiScale, c => Fill(c, bounds, Blue)), Blue);
        var clipped = CountAll(Render(dpiScale, c =>
        {
            c.PushClip(bounds);
            Fill(c, bounds, Blue);
            c.PopClip();
        }), Blue);

        Assert.True(unclipped > 0);
        Assert.Equal(unclipped, clipped);
    }

    [GpuTheory]
    [InlineData(1.25f)]
    [InlineData(1.5f)]
    public void TheCanvasFillsEveryDevicePixel(float dpiScale)
    {
        var image = Render(dpiScale, c => Fill(c, new RectF(0f, 0f, CanvasWidth, CanvasHeight), Blue));

        Assert.Equal(image.Width * image.Height, CountAll(image, Blue));
    }
}
