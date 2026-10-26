namespace ZGF.Gui.Immediate.Tests;

/// <summary>Frames drawn onto a raster canvas show what a window would, images included, for a screenshot.</summary>
public sealed class RasterFramesTests
{
    private const int Size = 40;
    private const uint Red = 0xFFFF0000u;

    /// <summary>A 1x1 blue image.</summary>
    private static readonly byte[] BluePixel = [0, 0, 255, 255];

    [Fact]
    public void A_frame_paints_its_boxes_and_the_images_it_was_given()
    {
        var frames = new RasterFrames<DefaultUiState>(new DefaultUiState(), static ui =>
        {
            using (ui.Row(new Box { Gap = 0f }))
            {
                ui.Leaf(new Box { Width = 20f, Height = 20f, Background = Red });
                if (!ui.HasImage("blue"))
                    ui.SetImage("blue", 1, 1, BluePixel);
                ui.Image("blue", 20f, 20f);
            }
        }, Size, Size);

        frames.Frame();
        var rgba = frames.ToRgbaTopDown();

        Assert.Equal((255, 0, 0), PixelAt(rgba, 10, 10));
        Assert.Equal((0, 0, 255), PixelAt(rgba, 30, 10));
    }

    private static (int, int, int) PixelAt(byte[] rgba, int x, int y)
    {
        var i = (y * Size + x) * 4;
        return (rgba[i], rgba[i + 1], rgba[i + 2]);
    }
}
