using ZGF.Fonts;
using ZGF.Geometry;
using ZGF.Gui.Testing;

namespace ZGF.Gui.Tests;

/// <summary>The raster canvas paints the images it is given, so a screenshot shows them where the
/// window would.</summary>
public sealed class RasterImageTests : IDisposable
{
    private const int CanvasSize = 40;

    private static string FontPath => Path.Combine(AppContext.BaseDirectory, "Assets", "Inter-Regular.ttf");

    private readonly FreeTypeFontBackend _fonts = new();

    public void Dispose() => _fonts.Dispose();

    private RasterCanvas CreateCanvas()
    {
        var font = _fonts.LoadFontFromFile(FontPath, 16);
        return new RasterCanvas(CanvasSize, CanvasSize, _fonts, font, 1f, 0u);
    }

    /// <summary>A 1x2 image: a red pixel over a blue one.</summary>
    private static readonly byte[] RedOverBlue = [255, 0, 0, 255, 0, 0, 255, 255];

    [Fact]
    public void An_image_is_painted_in_its_rect_top_row_up()
    {
        var canvas = CreateCanvas();
        canvas.CreateOrUpdateRgbaImage("picture", 1, 2, RedOverBlue);

        var rgba = Render(canvas, "picture");

        Assert.Equal((255, 0, 0), PixelAt(rgba, 10, 5));
        Assert.Equal((0, 0, 255), PixelAt(rgba, 10, 15));
        Assert.Equal((0, 0, 0), PixelAt(rgba, 30, 30));
    }

    [Fact]
    public void A_removed_image_is_no_longer_painted()
    {
        var canvas = CreateCanvas();
        canvas.CreateOrUpdateRgbaImage("picture", 1, 2, RedOverBlue);

        canvas.RemoveImage("picture");
        var rgba = Render(canvas, "picture");

        Assert.False(canvas.HasImage("picture"));
        Assert.Equal((0, 0, 0), PixelAt(rgba, 10, 5));
    }

    /// <summary>Draws the image over the canvas's top-left 20x20 points; the canvas counts up from its bottom.</summary>
    private static byte[] Render(RasterCanvas canvas, string imageId)
    {
        canvas.BeginFrame();
        canvas.DrawImage(new DrawImageInputs
        {
            Position = new RectF { Left = 0, Bottom = CanvasSize - 20, Width = 20, Height = 20 },
            ImageId = imageId,
            TintColor = 0xFFFFFFFFu,
            Rotation = 0f,
            ZIndex = 0,
        });
        canvas.EndFrame();
        return canvas.ToRgbaTopDown();
    }

    /// <summary>The color of the pixel <paramref name="x"/> across and <paramref name="y"/> down.</summary>
    private static (int, int, int) PixelAt(byte[] rgba, int x, int y)
    {
        var i = (y * CanvasSize + x) * 4;
        return (rgba[i], rgba[i + 1], rgba[i + 2]);
    }
}
