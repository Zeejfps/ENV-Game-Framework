using System.Diagnostics;
using PngSharp.Api;
using Xunit.Abstractions;
using ZGF.Geometry;
using ZGF.Gui.OpenGL;
using ZGF.Gui.Testing.Gpu;

namespace ZGF.Gui.Tests;

/// <summary>The platform's shipping canvas drawing into an offscreen target, read back under the pixel
/// contract: RGBA, rows top-down, premultiplied alpha.</summary>
[Collection(GpuCollection.Name)]
public sealed class CanvasSessionTests(GpuDeviceFixture fixture, ITestOutputHelper output)
{
    private GpuDevice Device => fixture.Device;

    private static void Fill(ICanvas canvas, RectF position, uint argb) =>
        canvas.DrawRect(new DrawRectInputs
        {
            Position = position,
            Style = new RectStyle { BackgroundColor = argb },
            ZIndex = 0,
        });

    [GpuFact]
    public void An_opaque_rect_reads_back_exactly_with_rows_top_down()
    {
        var clock = Stopwatch.StartNew();
        using var session = Device.CreateSession(200, 100);
        var created = clock.Elapsed;
        clock.Restart();
        session.Draw(c => Fill(c, new RectF(20, 10, 60, 30), 0xFF3366CCu));
        var drawn = clock.Elapsed;
        clock.Restart();
        var image = session.Read();
        var read = clock.Elapsed;
        output.WriteLine($"{Device.Description}; device {fixture.CreationTime.TotalMilliseconds:F1} ms, " +
                         $"session {created.TotalMilliseconds:F1} ms, first frame {drawn.TotalMilliseconds:F1} ms, " +
                         $"readback {read.TotalMilliseconds:F1} ms");

        Assert.Equal((200, 100), (image.Width, image.Height));
        for (var y = 0; y < image.Height; y++)
        for (var x = 0; x < image.Width; x++)
        {
            var inside = x is >= 20 and < 80 && y is >= 60 and < 90;
            Assert.Equal(inside ? new Rgba8(0x33, 0x66, 0xCC, 0xFF) : default, image[x, y]);
        }
    }

    [GpuFact]
    public void Translucent_draws_read_back_premultiplied_and_save_as_straight_alpha()
    {
        using var session = Device.CreateSession(160, 100);
        session.Draw(c =>
        {
            Fill(c, new RectF(0, 0, 80, 50), 0xFF0000FFu);
            Fill(c, new RectF(40, 20, 80, 20), 0x80FF0000u);
            c.DrawBoxShadow(new DrawBoxShadowInputs
            {
                Position = new RectF(110, 60, 30, 20),
                BorderRadius = BorderRadiusStyle.All(4f),
                Shadow = new BoxShadowStyle { Blur = 12f, Color = 0xC000FF00u },
                ZIndex = 0,
            });
        });
        var image = session.Read();

        AssertNear(new Rgba8(128, 0, 127, 255), image[60, 70], "translucent red over opaque blue");
        AssertNear(new Rgba8(128, 0, 0, 128), image[100, 70], "translucent red over nothing");
        Assert.Equal(new Rgba8(0, 0, 255, 255), image[10, 90]);

        var shadowPixels = 0;
        for (var y = 0; y < 40; y++)
        for (var x = 100; x < 160; x++)
        {
            var pixel = image[x, y];
            if (pixel.A == 0) continue;
            shadowPixels++;
            Assert.Equal(new Rgba8(0, pixel.A, 0, pixel.A), pixel);
        }
        Assert.True(shadowPixels > 100, $"only {shadowPixels} shadow pixels");
        Assert.Contains(Enumerable.Range(100, 60), x => image[x, 20].A is > 0 and < 0xC0);

        var path = Path.Combine(Path.GetTempPath(), "zgf-gpu-tests", "translucent.png");
        image.SavePng(path);
        output.WriteLine(path);
        var png = Png.DecodeFromFile(path);
        Assert.Equal((160u, 100u), (png.Ihdr.Width, png.Ihdr.Height));
        AssertNear(new Rgba8(255, 0, 0, 128), PngPixel(png, 100, 70), "straight translucent red");
        AssertNear(new Rgba8(128, 0, 127, 255), PngPixel(png, 60, 70), "opaque mix");
        for (var y = 0; y < 40; y++)
        for (var x = 100; x < 160; x++)
        {
            var pixel = PngPixel(png, x, y);
            Assert.Equal(pixel.A == 0 ? default : new Rgba8(0, 255, 0, pixel.A), pixel);
        }
    }

    [GpuFact]
    public void Text_draws_glyph_pixels_inside_its_rect()
    {
        using var session = Device.CreateSession(200, 60);
        session.Draw(c => c.DrawText(new DrawTextInputs
        {
            Position = new RectF(10, 20, 180, 24),
            Text = "Hello, GPU",
            Style = new TextStyle { TextColor = 0xFF000000u },
            ZIndex = 0,
        }), clearArgb: 0xFFFFFFFFu);
        var image = session.Read();

        var inked = 0;
        for (var y = 0; y < image.Height; y++)
        for (var x = 0; x < image.Width; x++)
        {
            if (image[x, y] == new Rgba8(255, 255, 255, 255)) continue;
            inked++;
            Assert.InRange(x, 10, 190);
            Assert.InRange(y, 60 - 44, 60 - 20);
        }
        Assert.True(inked > 100, $"only {inked} inked pixels");
    }

    [GpuFact]
    public void A_fractional_scale_gives_the_rounded_physical_size_and_fills_it()
    {
        using var session = Device.CreateSession(301, 157, dpiScale: 1.25f);
        session.Draw(c => Fill(c, new RectF(0, 0, 301, 157), 0xFFFF8000u));
        var image = session.Read();

        Assert.Equal((376, 196), (session.Size.PixelWidth, session.Size.PixelHeight));
        Assert.Equal((376, 196), (image.Width, image.Height));
        for (var y = 0; y < image.Height; y++)
        for (var x = 0; x < image.Width; x++)
            Assert.Equal(new Rgba8(0xFF, 0x80, 0x00, 0xFF), image[x, y]);
    }

    [OpenGlFact]
    public void The_OpenGL_canvas_framebuffer_has_the_targets_rounded_physical_size()
    {
        using var session = GlCanvasSession.Create(Device, 301, 157, dpiScale: 1.25f);
        session.Draw(c => Fill(c, new RectF(0, 0, 301, 157), 0xFFFF8000u));
        var (canvasWidth, canvasHeight) = Device.Run(() =>
        {
            ((OpenGlRenderedCanvas)session.Canvas).ReadFramebufferRgba(out var w, out var h);
            return (w, h);
        });

        Assert.Equal((376, 196), (canvasWidth, canvasHeight));
    }

    [GpuFact]
    public void A_resized_session_draws_and_reads_at_the_new_size()
    {
        using var session = Device.CreateSession(40, 40);
        session.Resize(120, 70);
        session.Draw(c => Fill(c, new RectF(0, 0, 120, 70), 0xFF00FFFFu));
        var image = session.Read();

        Assert.Equal((120, 70), (image.Width, image.Height));
        Assert.Equal(new Rgba8(0, 255, 255, 255), image[0, 0]);
        Assert.Equal(new Rgba8(0, 255, 255, 255), image[119, 69]);
    }

    [GpuFact]
    public void Sessions_alive_together_each_draw_with_their_own_clips()
    {
        using var large = Device.CreateSession(200, 100);
        using var small = Device.CreateSession(40, 40);
        small.Draw(c => Fill(c, new RectF(0, 0, 40, 40), 0xFF00FF00u));
        large.Draw(c => Fill(c, new RectF(0, 0, 200, 100), 0xFFFF0000u));
        var image = large.Read();

        Assert.Equal(new Rgba8(255, 0, 0, 255), image[5, 95]);
        Assert.Equal(new Rgba8(255, 0, 0, 255), image[195, 5]);
        Assert.Equal(new Rgba8(0, 255, 0, 255), small.Read()[20, 20]);
    }

    [GpuFact]
    public void A_session_created_and_drawn_inside_another_sessions_draw_leaves_both_frames_where_they_belong()
    {
        using var outer = Device.CreateSession(60, 40);
        ICanvasSession? inner = null;
        try
        {
            outer.Draw(c =>
            {
                Fill(c, new RectF(0, 0, 60, 40), 0xFFFF0000u);
                inner = Device.CreateSession(30, 20);
                inner.Draw(ic => Fill(ic, new RectF(0, 0, 30, 20), 0xFF00FF00u));
            });

            var outerImage = outer.Read();
            Assert.Equal(new Rgba8(255, 0, 0, 255), outerImage[0, 0]);
            Assert.Equal(new Rgba8(255, 0, 0, 255), outerImage[59, 39]);
            Assert.Equal(new Rgba8(0, 255, 0, 255), inner!.Read()[15, 10]);
        }
        finally
        {
            inner?.Dispose();
        }
    }

    [GpuFact]
    public void A_session_refuses_to_draw_resize_or_dispose_inside_its_own_draw()
    {
        using var session = Device.CreateSession(20, 20);
        session.Draw(c =>
        {
            Assert.Throws<InvalidOperationException>(() => session.Draw(_ => { }));
            Assert.Throws<InvalidOperationException>(() => session.Resize(10, 10));
            Assert.Throws<InvalidOperationException>(session.Dispose);
            Fill(c, new RectF(0, 0, 20, 20), 0xFF0000FFu);
        });

        Assert.Equal(new Rgba8(0, 0, 255, 255), session.Read()[10, 10]);
    }

    [GpuFact]
    public void Frames_begun_on_a_session_and_its_surfaces_end_each_into_its_own_target()
    {
        using var session = Device.CreateSession(60, 40);
        var surface = session.CreateSurface(30, 20, 2f);

        Device.Run(() =>
        {
            session.BeginFrame();
            surface.BeginFrame();
            Fill(session.Canvas, new RectF(0, 0, 60, 40), 0xFFFF0000u);
            Fill(surface.Canvas, new RectF(0, 0, 30, 20), 0xFF00FF00u);
            surface.EndFrame();
            session.EndFrame();
        });

        Assert.Equal(new Rgba8(255, 0, 0, 255), session.Read()[30, 20]);
        var image = surface.Read();
        Assert.Equal((60, 40), (image.Width, image.Height));
        Assert.Equal(new Rgba8(0, 255, 0, 255), image[59, 39]);
        Assert.Throws<InvalidOperationException>(() => surface.EndFrame());
    }

    [GpuFact]
    public void A_surface_draws_text_with_its_sessions_fonts_and_goes_when_the_session_does()
    {
        var session = Device.CreateSession(20, 20);
        var surface = session.CreateSurface(200, 60, 1f);
        Device.Run(() =>
        {
            surface.BeginFrame();
            surface.Canvas.DrawText(new DrawTextInputs
            {
                Position = new RectF(10, 20, 180, 24),
                Text = "Hello, surface",
                Style = new TextStyle { TextColor = 0xFF000000u },
                ZIndex = 0,
            });
            surface.EndFrame(0xFFFFFFFFu);
        });
        var image = surface.Read();
        session.Dispose();

        Assert.Contains(Enumerable.Range(0, image.Width * image.Height), i => image[i % image.Width, i / image.Width] != new Rgba8(255, 255, 255, 255));
        Assert.Throws<ObjectDisposedException>(() => surface.Read());
    }

    [GpuFact]
    public void A_surface_at_twice_its_sessions_scale_draws_text_at_the_same_size_in_points()
    {
        using var session = Device.CreateSession(200, 60);
        var surface = session.CreateSurface(200, 60, 2f);
        var style = new TextStyle { TextColor = 0xFF000000u };
        void Hello(ICanvas canvas) => canvas.DrawText(new DrawTextInputs { Position = new RectF(10, 10, 180, 40), Text = "Hello", Style = style, ZIndex = 0 });

        var (lineHeight, surfaceLineHeight) = Device.Run(() => (session.Canvas.MeasureTextLineHeight(style), surface.Canvas.MeasureTextLineHeight(style)));
        session.Draw(Hello, clearArgb: 0xFFFFFFFFu);
        Device.Run(() =>
        {
            surface.BeginFrame();
            Hello(surface.Canvas);
            surface.EndFrame(0xFFFFFFFFu);
        });

        Assert.Equal(lineHeight, surfaceLineHeight, 0.5f);
        var ratio = InkedRows(surface.Read()) / (float)InkedRows(session.Read());
        Assert.InRange(ratio, 1.7f, 2.3f);
    }

    private static int InkedRows(RgbaImage image) =>
        Enumerable.Range(0, image.Height).Count(y => Enumerable.Range(0, image.Width).Any(x => image[x, y] != new Rgba8(255, 255, 255, 255)));

    [Fact]
    public void A_size_whose_pixels_cannot_be_held_is_refused()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => TargetSize.Of(100_000, 100_000));
        Assert.Throws<ArgumentOutOfRangeException>(() => TargetSize.Of(int.MaxValue, 1, 2f));
        Assert.Throws<ArgumentOutOfRangeException>(() => TargetSize.Of(1_600_000_000, 1_600_000_000));
        Assert.Throws<ArgumentOutOfRangeException>(() => TargetSize.Of(1 << 29, 1));
        Assert.Equal(((1 << 29) - 64) * 4, TargetSize.Of((1 << 29) - 64, 1).ByteCount);
        Assert.Throws<ArgumentOutOfRangeException>(() => TargetSize.Of(10, 10, float.NaN));
        Assert.Equal(376 * 196 * 4, TargetSize.Of(301, 157, 1.25f).ByteCount);
    }

    [GpuFact]
    public void A_size_beyond_the_contexts_limits_is_refused_and_the_device_stays_usable()
    {
        var tooWide = Assert.Throws<ArgumentOutOfRangeException>(() => Device.CreateSession(70_000, 10));
        Assert.Contains("limit", tooWide.Message);

        using var session = Device.CreateSession(10, 10);
        Assert.Throws<ArgumentOutOfRangeException>(() => session.Resize(70_000, 10));
        session.Draw(c => Fill(c, new RectF(0, 0, 10, 10), 0xFFFFFFFFu));
        Assert.Equal(new Rgba8(255, 255, 255, 255), session.Read()[9, 9]);
    }

    private static Rgba8 PngPixel(IRawPng png, int x, int y)
    {
        var i = (y * (int)png.Ihdr.Width + x) * 4;
        var data = png.PixelData;
        return new Rgba8(data[i], data[i + 1], data[i + 2], data[i + 3]);
    }

    private static void AssertNear(Rgba8 expected, Rgba8 actual, string what)
    {
        var near = Math.Abs(expected.R - actual.R) <= 1 && Math.Abs(expected.G - actual.G) <= 1
                   && Math.Abs(expected.B - actual.B) <= 1 && Math.Abs(expected.A - actual.A) <= 1;
        Assert.True(near, $"{what}: expected {expected}, got {actual}");
    }
}
