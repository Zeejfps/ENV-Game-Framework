using System.Text;
using PngSharp.Api;
using ZGF.Gui.Desktop.Components.ContextMenu;
using ZGF.Gui.Desktop.Controllers;
using ZGF.Gui.Desktop.Input;
using ZGF.Gui.Desktop.Widgets;
using ZGF.Gui.Testing;
using ZGF.Gui.Testing.Gpu;
using ZGF.Gui.Widgets;

namespace ZGF.Gui.Tests;

/// <summary>A harness drawn by the shipping canvas on a GPU runs its tree on the render thread and writes what it
/// shows as a PNG.</summary>
[Collection(GpuCollection.Name)]
public sealed class GpuHarnessTests(GpuDeviceFixture fixture)
{
    [GpuFact]
    public void SaveScreenshot_WritesDecodablePng()
    {
        using var h = GuiTestHarness.CreateGpu(fixture.Device,
            ctx => new Box { Background = 0xFF202020u, Children = [new Text { Value = "Hello" }] }.BuildView(ctx),
            200, 80);
        h.Settle();

        var path = Path.Combine(Path.GetTempPath(), "zgf-gui-tests", "shot.png");
        h.SaveScreenshot(path);

        var png = Png.DecodeFromFile(path);
        Assert.Equal(200, png.GetDimensions().Width);
        Assert.Equal(80, png.GetDimensions().Height);
        var shown = h.Screenshot();
        Assert.Equal(shown.ToStraightAlpha(), png.PixelData);
        Assert.Equal(new Rgba8(0x20, 0x20, 0x20, 0xFF), shown[190, 70]);
    }

    [GpuFact]
    public void The_tree_is_built_and_driven_on_the_render_thread()
    {
        var device = fixture.Device;
        var builtOnRenderThread = false;
        var clickedOnRenderThread = false;
        using var h = GuiTestHarness.CreateGpu(device, ctx =>
        {
            builtOnRenderThread = device.IsRenderThread;
            return new KbmInput
            {
                OnClick = () => clickedOnRenderThread = device.IsRenderThread,
                Child = new Box { Children = [new Text { Value = "Push" }] },
            }.BuildView(ctx);
        }, 200, 80);

        h.Click("Push");

        Assert.True(builtOnRenderThread);
        Assert.True(clickedOnRenderThread);
    }

    [GpuFact]
    public void Menus_close_lookups_run_and_the_tree_is_disposed_on_the_render_thread()
    {
        var device = fixture.Device;
        var seen = new List<(string What, bool OnRenderThread)>();
        void Note(string what) => seen.Add((what, device.IsRenderThread));
        var h = GuiTestHarness.CreateGpu(device,
            ctx => new Box { Children = [new Text { Value = "Body" }] }.Use(_ => new OnDispose(() => Note("unmounted"))).BuildView(ctx),
            200, 80);
        device.Run(() =>
        {
            var opened = h.Context.Require<IContextMenuHost>().ShowContextMenu(ctx =>
            {
                var menu = new ContextMenu();
                var item = new ContextMenuItem(ctx.Canvas) { Text = "Fetch" };
                item.UseController(ctx.Require<InputSystem>(), () => new ContextMenuItemDefaultKbmController(item, ctx, () => Note("item")));
                menu.Children.Add(item);
                return menu;
            }, default);
            opened!.Closed += () => Note("closed");
        });

        h.ClickMenuItem("Fetch");
        var open = h.OpenMenuCount;
        var missing = Assert.Throws<InvalidOperationException>(() => h.Get("missing"));
        h.Dispose();

        Assert.Equal(0, open);
        Assert.Contains("Body", missing.Message);
        Assert.Equal([("item", true), ("closed", true), ("unmounted", true)], seen);
    }

    [GpuFact]
    public void A_resized_harness_draws_and_reads_at_the_new_size()
    {
        using var h = GuiTestHarness.CreateGpu(fixture.Device,
            ctx => new Box { Background = 0xFF2040A0u }.BuildView(ctx), 100, 50, clearColor: 0xFF000000u);
        var fill = new Rgba8(0x20, 0x40, 0xA0, 0xFF);

        h.Resize(160, 90);
        var shown = h.Screenshot();

        Assert.Equal((160, 90), (shown.Width, shown.Height));
        Assert.Equal(fill, shown[5, 5]);
        Assert.Equal(fill, shown[155, 85]);
        Assert.Equal(fill, shown[155, 5]);
    }

    private static readonly byte[] Disc = Encoding.UTF8.GetBytes(
        """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><circle cx="12" cy="12" r="10" fill="currentColor"/></svg>""");

    private static readonly byte[] Square = Encoding.UTF8.GetBytes(
        """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><rect width="24" height="24"/></svg>""");

    [GpuFact]
    public void An_unchanged_icon_whose_raster_the_cache_evicted_is_drawn_again()
    {
        var time = new ManualTime();
        var cache = new SvgImageCache(new SvgImageCacheOptions { MaxRasterBytes = 32 * 32 * 4, Time = time });
        using var h = GuiTestHarness.CreateGpu(fixture.Device,
            ctx => new SvgImage { Data = Disc, Color = 0xFF00FF00u }.BuildView(ctx), 32, 32,
            configure: ctx => ctx.AddService(cache), clearColor: 0xFF000000u);
        var green = new Rgba8(0, 0xFF, 0, 0xFF);
        Assert.Equal(green, h.Screenshot()[16, 16]);

        time.Milliseconds += 600;
        fixture.Device.Run(() => cache.Acquire(h.Context.Canvas, cache.GetOrParse(Square), 32, 32, 0xFF000000u));
        var rasters = cache.RasterCount;
        var shown = h.Screenshot();

        Assert.Equal(green, shown[16, 16]);
        Assert.Equal(rasters + 1, cache.RasterCount);
    }

    [GpuFact]
    public void An_icon_the_cache_evicts_later_in_the_frame_that_drew_it_is_still_drawn()
    {
        var time = new ManualTime { StepMilliseconds = 600 };
        var cache = new SvgImageCache(new SvgImageCacheOptions { MaxRasterBytes = 32 * 32 * 4, Time = time });
        using var h = GuiTestHarness.CreateGpu(fixture.Device,
            ctx => new Row
            {
                Children =
                [
                    new SvgImage { Data = Disc, Color = 0xFF00FF00u, Width = 32, Height = 32 },
                    new SvgImage { Data = Disc, Color = 0xFFFF0000u, Width = 32, Height = 32 },
                ],
            }.BuildView(ctx), 64, 32,
            configure: ctx => ctx.AddService(cache), clearColor: 0xFF000000u);

        var shown = h.Screenshot();

        Assert.Equal(new Rgba8(0, 0xFF, 0, 0xFF), shown[16, 16]);
        Assert.Equal(new Rgba8(0xFF, 0, 0, 0xFF), shown[48, 16]);
    }

    private sealed class OnDispose(Action disposed) : IDisposable
    {
        public void Dispose() => disposed();
    }

    private sealed class ManualTime : TimeProvider
    {
        public long Milliseconds;
        public long StepMilliseconds;

        public override long TimestampFrequency => 1000;

        public override long GetTimestamp() => Milliseconds += StepMilliseconds;
    }
}
