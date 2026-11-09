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

    private static readonly byte[] RedPixel = [255, 0, 0, 255];
    private static readonly byte[] BluePixel = [0, 0, 255, 255];

    [GpuFact]
    public void A_removed_image_is_gone_at_once()
    {
        using var session = Device.CreateSession(CanvasSize, CanvasSize);
        session.BeginFrame();

        var (has, size) = Device.Run(() =>
        {
            var canvas = session.Canvas;
            canvas.CreateOrUpdateRgbaImage("picture", 1, 2, RedOverBlue);
            Stage(canvas, "picture", left: 0);
            canvas.RemoveImage("picture");
            Stage(canvas, "picture", left: 20);
            return (canvas.HasImage("picture"), canvas.GetImageSize("picture"));
        });
        session.EndFrame();
        var image = session.Read();

        Assert.False(has);
        Assert.Equal(default, size);
        Assert.Equal(Red, image[10, 4]);
        Assert.Equal(default, image[30, 4]);
    }

    [GpuFact]
    public void An_image_removed_and_given_again_in_one_frame_draws_the_old_pixels_where_staged_before_and_the_new_after()
    {
        using var session = Device.CreateSession(CanvasSize, CanvasSize);
        session.BeginFrame();
        Device.Run(() =>
        {
            var canvas = session.Canvas;
            canvas.CreateOrUpdateRgbaImage("picture", 1, 1, RedPixel);
            Stage(canvas, "picture", left: 0);
            canvas.RemoveImage("picture");
            canvas.CreateOrUpdateRgbaImage("picture", 1, 1, BluePixel);
            Stage(canvas, "picture", left: 20);
        });
        session.EndFrame();
        var first = session.Read();
        var next = Render(session, "picture");

        Assert.Equal(Red, first[10, 10]);
        Assert.Equal(Blue, first[30, 10]);
        Assert.Equal(Blue, next[10, 10]);
        Assert.Equal(0, session.RetiredImageCount);
    }

    [GpuFact]
    public void An_image_one_canvas_removes_mid_frame_and_another_gives_again_survives_the_first_frame_ending()
    {
        using var session = Device.CreateSession(CanvasSize, CanvasSize);
        using var popup = session.CreateSurface(CanvasSize, CanvasSize, 1f);
        Device.Run(() => session.Canvas.CreateOrUpdateRgbaImage("picture", 1, 1, RedPixel));

        session.BeginFrame();
        Device.Run(() =>
        {
            Stage(session.Canvas, "picture", left: 0);
            session.Canvas.RemoveImage("picture");
        });
        popup.BeginFrame();
        Device.Run(() =>
        {
            popup.Canvas.CreateOrUpdateRgbaImage("picture", 1, 1, BluePixel);
            Stage(popup.Canvas, "picture", left: 0);
        });
        popup.EndFrame();
        session.EndFrame();
        var main = session.Read();
        var shown = popup.Read();
        var again = Render(session, "picture");

        Assert.Equal(Red, main[10, 10]);
        Assert.Equal(Blue, shown[10, 10]);
        Assert.Equal(Blue, again[10, 10]);
        Assert.True(Device.Run(() => popup.Canvas.HasImage("picture")));
        Assert.Equal(0, session.RetiredImageCount);
    }

    [GpuFact]
    public void A_texture_removed_while_a_frame_is_open_is_freed_when_that_frame_is_abandoned()
    {
        using var session = Device.CreateSession(CanvasSize, CanvasSize);
        var popup = session.CreateSurface(CanvasSize, CanvasSize, 1f);

        RemoveDuringFrame(session, popup);
        Assert.Equal(1, session.RetiredImageCount);
        popup.Resize(CanvasSize, CanvasSize);
        Assert.Equal(0, session.RetiredImageCount);

        RemoveDuringFrame(session, popup);
        popup.BeginFrame();
        Assert.Equal(0, session.RetiredImageCount);
        popup.EndFrame();

        RemoveDuringFrame(session, popup);
        popup.Dispose();
        Assert.Equal(0, session.RetiredImageCount);

        Device.Run(() => session.Canvas.CreateOrUpdateRgbaImage("picture", 1, 1, RedPixel));
        Assert.Throws<InvalidOperationException>(() => session.Draw(canvas =>
        {
            Stage(canvas, "picture", left: 0);
            canvas.RemoveImage("picture");
            throw new InvalidOperationException("drawing failed");
        }));
        Assert.Equal(0, session.RetiredImageCount);
    }

    [GpuFact]
    public void A_texture_a_popup_last_drew_lives_through_its_present_only_redraws_until_it_begins_another_frame()
    {
        using var session = Device.CreateSession(CanvasSize, CanvasSize);
        using var popup = session.CreateSurface(CanvasSize, CanvasSize, 1f);
        Device.Run(() => session.Canvas.CreateOrUpdateRgbaImage("picture", 1, 1, RedPixel));
        popup.BeginFrame();
        Device.Run(() => Stage(popup.Canvas, "picture", left: 0));
        popup.EndFrame();

        Device.Run(() => session.Canvas.RemoveImage("picture"));
        Render(session, "picture");
        Render(session, "picture");
        popup.Replay();
        var replayed = popup.Read();
        var keptWhileReplayable = session.RetiredImageCount;
        popup.BeginFrame();
        var keptAfterMovingOn = session.RetiredImageCount;
        popup.EndFrame();

        Assert.Equal(Red, replayed[10, 10]);
        Assert.Equal(1, keptWhileReplayable);
        Assert.Equal(0, keptAfterMovingOn);
    }

    [GpuFact]
    public void An_idle_window_keeps_only_the_textures_it_drew()
    {
        using var session = Device.CreateSession(CanvasSize, CanvasSize);
        using var idle = session.CreateSurface(CanvasSize, CanvasSize, 1f);
        Device.Run(() =>
        {
            session.Canvas.CreateOrUpdateRgbaImage("shown", 1, 1, RedPixel);
            session.Canvas.CreateOrUpdateRgbaImage("other", 1, 1, BluePixel);
        });
        idle.BeginFrame();
        Device.Run(() => Stage(idle.Canvas, "shown", left: 0));
        idle.EndFrame();
        Render(session, "other");

        Device.Run(() => session.Canvas.RemoveImage("shown"));
        Device.Run(() => session.Canvas.RemoveImage("other"));
        var whileIdleHoldsOnlyItsOwn = session.RetiredImageCount;
        Render(session, "nothing");
        var afterTheMainCanvasMovesOn = session.RetiredImageCount;
        idle.Replay();
        var replayed = idle.Read();
        idle.BeginFrame();
        var afterTheIdleWindowMovesOn = session.RetiredImageCount;
        idle.EndFrame();

        Assert.Equal(2, whileIdleHoldsOnlyItsOwn);
        Assert.Equal(1, afterTheMainCanvasMovesOn);
        Assert.Equal(Red, replayed[10, 10]);
        Assert.Equal(0, afterTheIdleWindowMovesOn);
    }

    [GpuFact]
    public void Resizing_a_canvas_drops_the_frame_it_ended_and_keeps_the_one_it_is_staging()
    {
        using var session = Device.CreateSession(CanvasSize, CanvasSize);
        using var popup = session.CreateSurface(CanvasSize, CanvasSize, 1f);
        Device.Run(() => session.Canvas.CreateOrUpdateRgbaImage("ended", 1, 1, RedPixel));
        popup.BeginFrame();
        Device.Run(() => Stage(popup.Canvas, "ended", left: 0));
        popup.EndFrame();
        Device.Run(() => session.Canvas.RemoveImage("ended"));
        var heldBeforeResize = session.RetiredImageCount;

        Device.Run(() => popup.Canvas.Resize(CanvasSize, CanvasSize + 10));
        var heldAfterResize = session.RetiredImageCount;

        Device.Run(() => session.Canvas.CreateOrUpdateRgbaImage("staging", 1, 1, BluePixel));
        popup.BeginFrame();
        Device.Run(() =>
        {
            Stage(popup.Canvas, "staging", left: 0);
            popup.Canvas.Resize(CanvasSize, CanvasSize);
        });
        popup.EndFrame();
        var drawn = popup.Read();

        Assert.Equal(1, heldBeforeResize);
        Assert.Equal(0, heldAfterResize);
        Assert.Equal(Blue, drawn[10, 10]);
    }

    [GpuFact]
    public void A_shared_texture_the_canvas_cannot_import_leaves_a_removed_image_removed()
    {
        using var session = Device.CreateSession(CanvasSize, CanvasSize);

        var (imported, has) = Device.Run(() =>
        {
            var canvas = session.Canvas;
            canvas.CreateOrUpdateRgbaImage("picture", 1, 1, RedPixel);
            canvas.RemoveImage("picture");
            var kind = canvas.SupportsSharedTextures(SharedTextureKind.IOSurface) ? SharedTextureKind.D3D11NtHandle : SharedTextureKind.IOSurface;
            var result = canvas.CreateOrUpdateSharedTextureImage("picture", new SharedTexture(kind, 0, 1, 1));
            return (result, canvas.HasImage("picture"));
        });
        var image = Render(session, "picture");

        Assert.False(imported);
        Assert.False(has);
        Assert.Equal(default, image[10, 10]);
    }

    private void RemoveDuringFrame(ICanvasSession session, ICanvasSurface popup)
    {
        Device.Run(() => session.Canvas.CreateOrUpdateRgbaImage("picture", 1, 1, RedPixel));
        popup.BeginFrame();
        Device.Run(() =>
        {
            Stage(popup.Canvas, "picture", left: 0);
            popup.Canvas.RemoveImage("picture");
        });
    }

    private static void Stage(RenderedCanvasBase canvas, string imageId, float left) => canvas.DrawImage(new DrawImageInputs
    {
        Position = new RectF { Left = left, Bottom = CanvasSize - 20, Width = 20, Height = 20 },
        ImageId = imageId,
        TintColor = 0xFFFFFFFFu,
        Rotation = 0f,
        ZIndex = 0,
    });

    /// <summary>Draws the image over the canvas's top-left 20x20 points; the canvas counts up from its bottom.</summary>
    private static RgbaImage Render(ICanvasSession session, string imageId)
    {
        session.Draw(canvas => Stage(canvas, imageId, left: 0));
        return session.Read();
    }
}
