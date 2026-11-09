using ZGF.Geometry;
using ZGF.Gui.Testing.Gpu;

namespace ZGF.Gui.Tests;

/// <summary>The shipping canvas paints the images it is given where it is told to.</summary>
[Collection(GpuCollection.Name)]
public sealed class CanvasImageTests(GpuDeviceFixture fixture)
{
    private const int CanvasSize = 40;

    /// <summary>A 1x2 image: a red pixel over a blue one.</summary>
    private static readonly byte[] RedOverBlue = [255, 0, 0, 255, 0, 0, 255, 255];

    private static readonly Rgba8 Red = new(255, 0, 0, 255);
    private static readonly Rgba8 Blue = new(0, 0, 255, 255);

    private GpuDevice Device => fixture.Device;

    [GpuFact]
    public void An_image_is_painted_in_its_rect_top_row_up()
    {
        using var session = Device.CreateSession(CanvasSize, CanvasSize);
        Device.Run(() => session.Canvas.CreateOrUpdateRgbaImage("picture", 1, 2, RedOverBlue));

        var image = Render(session, "picture");

        Assert.Equal(Red, image[10, 4]);
        Assert.Equal(Blue, image[10, 15]);
        Assert.Equal(default, image[30, 30]);
    }

    [GpuFact]
    public void An_image_the_canvas_does_not_have_draws_nothing()
    {
        using var session = Device.CreateSession(CanvasSize, CanvasSize);
        Device.Run(() => session.Canvas.CreateOrUpdateRgbaImage("picture", 1, 2, RedOverBlue));

        Device.Run(() => session.Canvas.RemoveImage("picture"));
        var removed = Render(session, "picture");
        var unknown = Render(session, "never-given");

        Assert.Equal(default, removed[10, 4]);
        Assert.Equal(default, removed[10, 15]);
        Assert.Equal(default, unknown[10, 4]);
        Assert.Equal(default, unknown[10, 15]);
    }

    [GpuFact]
    public void A_removed_image_is_gone_and_its_id_paints_what_it_is_given_next()
    {
        using var session = Device.CreateSession(CanvasSize, CanvasSize);
        Device.Run(() => session.Canvas.CreateOrUpdateRgbaImage("picture", 1, 2, RedOverBlue));

        Device.Run(() => session.Canvas.RemoveImage("picture"));
        Assert.False(Device.Run(() => session.Canvas.HasImage("picture")));
        Device.Run(() => session.Canvas.CreateOrUpdateRgbaImage("picture", 1, 1, [0, 0, 255, 255]));
        var image = Render(session, "picture");

        Assert.Equal(Blue, image[10, 4]);
        Assert.Equal(Blue, image[10, 15]);
    }

    /// <summary>Draws the image over the canvas's top-left 20x20 points; the canvas counts up from its bottom.</summary>
    private static RgbaImage Render(ICanvasSession session, string imageId)
    {
        session.Draw(canvas => canvas.DrawImage(new DrawImageInputs
        {
            Position = new RectF { Left = 0, Bottom = CanvasSize - 20, Width = 20, Height = 20 },
            ImageId = imageId,
            TintColor = 0xFFFFFFFFu,
            Rotation = 0f,
            ZIndex = 0,
        }));
        return session.Read();
    }
}
