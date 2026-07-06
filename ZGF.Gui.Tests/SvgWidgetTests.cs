using System.Text;
using ZGF.Gui.Testing;
using ZGF.Gui.Widgets;
using ZGF.Observable;

namespace ZGF.Gui.Tests;

public sealed class SvgWidgetTests
{
    private static readonly byte[] SquareIcon = Encoding.UTF8.GetBytes(
        """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><circle cx="12" cy="12" r="10" fill="currentColor"/></svg>""");

    private static readonly byte[] WideIcon = Encoding.UTF8.GetBytes(
        """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 48 24"><rect width="48" height="24"/></svg>""");

    private static SvgImageCache CacheOf(GuiTestHarness harness) => harness.Context.Require<SvgImageCache>();
    private static FrameTicker TickerOf(GuiTestHarness harness) => (FrameTicker)harness.Context.Require<IFrameTicker>();

    [Fact]
    public void IntrinsicSizeComesFromViewBox()
    {
        var harness = GuiTestHarness.Create(
            ctx => new SvgImage { Data = WideIcon }.BuildView(ctx));
        var view = harness.Root;
        view.Width = StyleValue<float>.Unset;
        view.Height = StyleValue<float>.Unset;
        Assert.Equal(48f, view.MeasureWidth());
        Assert.Equal(24f, view.MeasureHeight());
    }

    [Fact]
    public void DrawsImageWithSyntheticIdEncodingSizeAndColor()
    {
        var harness = GuiTestHarness.Create(
            ctx => new SvgImage { Data = SquareIcon, Color = 0xFF336699 }.BuildView(ctx),
            width: 100, height: 100);

        var canvas = harness.Render();
        var image = Assert.Single(canvas.Images);
        Assert.Equal("svg:1@100x100#FF336699", image.Inputs.ImageId);
        Assert.Equal(1, CacheOf(harness).RasterCount);
    }

    [Fact]
    public void NonSquareIconRastersAtAspectFitSize()
    {
        var harness = GuiTestHarness.Create(
            ctx => new SvgImage { Data = WideIcon }.BuildView(ctx),
            width: 100, height: 100);

        var canvas = harness.Render();
        var image = Assert.Single(canvas.Images);
        // 2:1 icon fit into 100x100 → 100x50 raster.
        Assert.Equal("svg:1@100x50#FF000000", image.Inputs.ImageId);
    }

    [Fact]
    public void RepeatedRendersDoNotReRasterize()
    {
        var harness = GuiTestHarness.Create(
            ctx => new SvgImage { Data = SquareIcon }.BuildView(ctx),
            width: 64, height: 64);

        harness.Render();
        harness.Render();
        harness.Render();
        Assert.Equal(1, CacheOf(harness).RasterCount);
    }

    [Fact]
    public void TwoWidgetsWithSameSourceAndSizeShareOneRaster()
    {
        var harness = GuiTestHarness.Create(
            ctx => new Row
            {
                Children =
                [
                    new SvgImage { Data = SquareIcon, Width = 32, Height = 32 },
                    new SvgImage { Data = SquareIcon, Width = 32, Height = 32 },
                ],
            }.BuildView(ctx),
            width: 100, height: 32);

        var canvas = harness.Render();
        Assert.Equal(2, canvas.Images.Count);
        Assert.Equal(canvas.Images[0].Inputs.ImageId, canvas.Images[1].Inputs.ImageId);
        Assert.Equal(1, CacheOf(harness).RasterCount);
    }

    [Fact]
    public void ModerateResizeDebouncesThenReRastersOnce()
    {
        var harness = GuiTestHarness.Create(
            ctx => new SvgImage { Data = SquareIcon }.BuildView(ctx),
            width: 100, height: 100);
        harness.Render();
        Assert.Equal(1, CacheOf(harness).RasterCount);

        // +50%: below the 2x escape hatch, so the stale raster keeps drawing.
        harness.Resize(150, 150);
        var canvas = harness.Render();
        Assert.Equal("svg:1@100x100#FF000000", Assert.Single(canvas.Images).Inputs.ImageId);
        Assert.Equal(1, CacheOf(harness).RasterCount);
        Assert.True(TickerOf(harness).ActiveCount > 0);

        // Size stays stable past the settle delay → exactly one re-raster at the exact size.
        harness.Advance(0.2f);
        canvas = harness.Render();
        Assert.Equal("svg:1@150x150#FF000000", Assert.Single(canvas.Images).Inputs.ImageId);
        Assert.Equal(2, CacheOf(harness).RasterCount);
        Assert.Equal(0, TickerOf(harness).ActiveCount);
    }

    [Fact]
    public void ResizeBeyondTwoTimesReRastersImmediately()
    {
        var harness = GuiTestHarness.Create(
            ctx => new SvgImage { Data = SquareIcon }.BuildView(ctx),
            width: 100, height: 100);
        harness.Render();

        harness.Resize(250, 250);
        var canvas = harness.Render();
        Assert.Equal("svg:1@250x250#FF000000", Assert.Single(canvas.Images).Inputs.ImageId);
        Assert.Equal(2, CacheOf(harness).RasterCount);
        Assert.Equal(0, TickerOf(harness).ActiveCount);
    }

    [Fact]
    public void ContinuedResizingKeepsDeferringUntilStable()
    {
        var harness = GuiTestHarness.Create(
            ctx => new SvgImage { Data = SquareIcon }.BuildView(ctx),
            width: 100, height: 100);
        harness.Render();

        // Animate: size keeps changing every few frames — never settles, never re-rasters.
        for (var size = 105; size <= 150; size += 5)
        {
            harness.Resize(size, size);
            harness.Render();
            harness.Advance(0.05f);
            harness.Render();
        }
        Assert.Equal(1, CacheOf(harness).RasterCount);

        // Stop animating: settles once.
        harness.Advance(0.2f);
        harness.Render();
        Assert.Equal(2, CacheOf(harness).RasterCount);
        Assert.Equal(0, TickerOf(harness).ActiveCount);
    }

    [Fact]
    public void ColorRebindReRastersWithNewId()
    {
        var color = new State<uint>(0xFF111111);
        var harness = GuiTestHarness.Create(
            ctx => new SvgImage { Data = SquareIcon, Color = color }.BuildView(ctx),
            width: 64, height: 64);

        var canvas = harness.Render();
        Assert.Equal("svg:1@64x64#FF111111", Assert.Single(canvas.Images).Inputs.ImageId);

        color.Value = 0xFF222222;
        canvas = harness.Render();
        Assert.Equal("svg:1@64x64#FF222222", Assert.Single(canvas.Images).Inputs.ImageId);
        Assert.Equal(2, CacheOf(harness).RasterCount);
    }

    [Fact]
    public void ColorChangeOnDocumentWithoutCurrentColorSharesRaster()
    {
        var color = new State<uint>(0xFF111111);
        var fixedColorIcon = Encoding.UTF8.GetBytes(
            """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><circle cx="12" cy="12" r="10" fill="red"/></svg>""");
        var harness = GuiTestHarness.Create(
            ctx => new SvgImage { Data = fixedColorIcon, Color = color }.BuildView(ctx),
            width: 64, height: 64);

        harness.Render();
        color.Value = 0xFF222222;
        harness.Render();
        // The document never references currentColor, so recoloring shares one raster.
        Assert.Equal(1, CacheOf(harness).RasterCount);
    }

    [Fact]
    public void MissingSourceAndDataThrows()
    {
        Assert.Throws<InvalidOperationException>(() =>
            GuiTestHarness.Create(ctx => new SvgImage().BuildView(ctx)));
        Assert.Throws<InvalidOperationException>(() =>
            GuiTestHarness.Create(ctx => new SvgImage { Source = "a.svg", Data = [1] }.BuildView(ctx)));
    }

    [Fact]
    public void UnmountStopsSettleTick()
    {
        var harness = GuiTestHarness.Create(
            ctx => new SvgImage { Data = SquareIcon }.BuildView(ctx),
            width: 100, height: 100);
        harness.Render();
        harness.Resize(150, 150);
        harness.Render();
        Assert.True(TickerOf(harness).ActiveCount > 0);

        harness.Root.Unmount();
        Assert.Equal(0, TickerOf(harness).ActiveCount);
    }
}
