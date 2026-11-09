using ZGF.Desktop;
using ZGF.Fonts;
using ZGF.Geometry;
using ZGF.Gui.Hosting;
using ZGF.Gui.Testing;

namespace ZGF.Gui.Tests;

/// <summary>A canvas frame a host abandons is let go of, so nothing it staged is drawn again.</summary>
public sealed class HostFramesTests : IDisposable
{
    private readonly FreeTypeFontBackend _fonts = new();
    private readonly FontHandle _font;

    public HostFramesTests()
    {
        _font = _fonts.LoadFontFromFile(Path.Combine(AppContext.BaseDirectory, "Assets", "Inter-Regular.ttf"), 16);
    }

    public void Dispose() => _fonts.Dispose();

    [Fact]
    public void A_host_frame_that_throws_abandons_the_canvas_frames_it_began_and_no_others()
    {
        var shown = Canvas();
        var begun = Canvas();
        var frames = new HostFrames();
        frames.Add(shown);
        frames.Add(begun);
        DrawPicture(shown);

        Assert.Throws<InvalidOperationException>(() => frames.Run(() =>
        {
            begun.BeginFrame();
            Stage(begun);
            throw new InvalidOperationException("the frame failed");
        }));
        shown.EndFrame();
        begun.EndFrame();

        Assert.Single(shown.Images);
        Assert.Empty(begun.Images);
    }

    [Fact]
    public void A_frame_run_inside_another_that_throws_abandons_its_own_canvases_while_the_outer_frame_goes_on()
    {
        var main = Canvas();
        var popup = Canvas();
        var frames = new HostFrames();
        frames.Add(main);
        frames.Add(popup);

        frames.Run(() =>
        {
            main.BeginFrame();
            Stage(main);
            try
            {
                frames.Run(() =>
                {
                    popup.BeginFrame();
                    Stage(popup);
                    throw new InvalidOperationException("the popup failed");
                });
            }
            catch (InvalidOperationException)
            {
            }
            main.EndFrame();
        });
        popup.EndFrame();

        Assert.Single(main.Images);
        Assert.Empty(popup.Images);
    }

    [Fact]
    public void A_popup_put_back_in_the_pool_draws_nothing_when_presented_again()
    {
        var app = new FakeWindowedApp([new MonitorWorkArea(0, 0, 1920, 1080)]);
        var pool = new PopupPool(new FakeRenderBackend(app, _fonts, _font), capacity: 4);
        var popup = pool.Acquire(mousePassThrough: false);
        var canvas = (CaptureCanvas)popup.Canvas;
        canvas.ImageSizes["picture"] = new Size { Width = 1, Height = 1 };
        DrawPicture(canvas);

        pool.Release(popup);
        canvas.EndFrame();

        Assert.Empty(canvas.Images);
    }

    private CaptureCanvas Canvas()
    {
        var canvas = new CaptureCanvas(_fonts, _font);
        canvas.ImageSizes["picture"] = new Size { Width = 1, Height = 1 };
        return canvas;
    }

    private static void DrawPicture(CaptureCanvas canvas)
    {
        canvas.BeginFrame();
        Stage(canvas);
        canvas.EndFrame();
    }

    private static void Stage(RenderedCanvasBase canvas) => canvas.DrawImage(new DrawImageInputs
    {
        Position = new RectF { Left = 0, Bottom = 0, Width = 10, Height = 10 },
        ImageId = "picture",
        TintColor = 0xFFFFFFFFu,
        Rotation = 0f,
        ZIndex = 0,
    });
}
