using ZGF.Fonts;
using ZGF.Gui.Desktop;
using ZGF.Gui.Views;

namespace ZGF.Gui.Tests;

/// <summary>
/// One factor from logical points to device pixels, followed through a window: the canvas is the
/// framebuffer divided by the scale, the root is the canvas, and a change to either half of the
/// factor moves both.
/// </summary>
public sealed class UiScaleTests : IDisposable
{
    private static string FontPath => Path.Combine(AppContext.BaseDirectory, "Assets", "Inter-Regular.ttf");

    private readonly FreeTypeFontBackend _fonts = new();
    private readonly FontHandle _font;

    public UiScaleTests()
    {
        _font = _fonts.LoadFontFromFile(FontPath, 16);
    }

    public void Dispose() => _fonts.Dispose();

    private (GuiWindowHost Host, FakeWindow Window, FakeUiScale Scale) Host(
        int width = 1600, int height = 900, float uiScale = 1f, float backingRatio = 1f,
        float? contentScale = null)
    {
        var window = new FakeWindow(width, height, backingRatio, contentScale);
        var canvas = new CaptureCanvas(_fonts, _font);
        var scale = new FakeUiScale(uiScale);
        var input = new DesktopInputSystem(window, scale);
        var host = new GuiWindowHost(window, canvas, input, new Context(), scale, sizeRootToWindow: true);
        return (host, window, scale);
    }

    [Theory]
    [InlineData(1f, 1600, 900)]
    [InlineData(1.25f, 1280, 720)]
    [InlineData(1.5f, 1067, 600)]
    [InlineData(2f, 800, 450)]
    public void TheCanvasIsTheFramebufferDividedByTheScale(float uiScale, int expectedWidth, int expectedHeight)
    {
        var (host, _, _) = Host(uiScale: uiScale);

        Assert.Equal(expectedWidth, host.Canvas.Width);
        Assert.Equal(expectedHeight, host.Canvas.Height);
        Assert.Equal(uiScale, host.Canvas.DpiScale);
    }

    // The macOS regression guard: a Retina window must lay out at its point size, not at half of it.
    [Fact]
    public void ARetinaWindowLaysOutAtItsPointSize()
    {
        var (host, _, _) = Host(width: 800, height: 600, uiScale: 1f, backingRatio: 2f);

        Assert.Equal(800, host.Canvas.Width);
        Assert.Equal(600, host.Canvas.Height);
        Assert.Equal(2f, host.Canvas.DpiScale);
    }

    [Theory]
    [InlineData(1.25f)]
    [InlineData(1.5f)]
    [InlineData(2f)]
    public void ARetinaWindowStillDividesByTheUsersScale(float uiScale)
    {
        var (host, _, _) = Host(width: 800, height: 600, uiScale: uiScale, backingRatio: 2f);

        Assert.Equal((int)MathF.Round(800 / uiScale), host.Canvas.Width);
        Assert.Equal(2f * uiScale, host.Canvas.DpiScale);
    }

    // The Windows case the whole exercise exists for: the framebuffer is the window's pixel size at
    // every display setting, so the scaling only reaches the app through the content scale.
    [Theory]
    [InlineData(1.25f)]
    [InlineData(1.5f)]
    [InlineData(2f)]
    public void AMonitorsContentScaleCountsForTheSameAsTheUsersScale(float scale)
    {
        var fromMonitor = Host(uiScale: 1f, contentScale: scale).Host;
        var fromUser = Host(uiScale: scale, contentScale: 1f).Host;

        Assert.Equal(fromUser.Canvas.Width, fromMonitor.Canvas.Width);
        Assert.Equal(fromUser.Canvas.Height, fromMonitor.Canvas.Height);
        Assert.Equal(fromUser.Canvas.DpiScale, fromMonitor.Canvas.DpiScale);
    }

    // The two ways a platform can report "2" must not compound: a Retina panel reports it as a
    // backing ratio and a scaled Windows display reports it as a content scale, and both mean one
    // logical point is two device pixels — not four.
    [Fact]
    public void TheTwoWaysOfReportingTwoDoNotCompound()
    {
        var retina = Host(width: 800, height: 600, backingRatio: 2f).Host;
        var scaledFlat = Host(width: 1600, height: 1200, backingRatio: 1f, contentScale: 2f).Host;

        Assert.Equal(retina.Canvas.Width, scaledFlat.Canvas.Width);
        Assert.Equal(retina.Canvas.DpiScale, scaledFlat.Canvas.DpiScale);
    }

    [Fact]
    public void TheTwoScalesMultiply()
    {
        var (host, _, _) = Host(uiScale: 2f, contentScale: 1.5f);

        Assert.Equal(3f, host.Canvas.DpiScale);
        Assert.Equal((int)MathF.Round(1600 / 3f), host.Canvas.Width);
    }

    [Fact]
    public void ATinyWindowStillHasANonZeroCanvas()
    {
        var (host, _, _) = Host(width: 1, height: 1, uiScale: 2f);

        Assert.True(host.Canvas.Width >= 1);
        Assert.True(host.Canvas.Height >= 1);
    }

    [Fact]
    public void AResizeFollowsTheNewFramebuffer()
    {
        var (host, window, _) = Host(uiScale: 2f);

        window.SetSize(1000, 600);
        host.SyncScale();

        Assert.Equal(500, host.Canvas.Width);
        Assert.Equal(300, host.Canvas.Height);
    }

    [Fact]
    public void TheRootFillsTheCanvas()
    {
        var (host, _, _) = Host(uiScale: 2f);
        var root = new ContainerView();

        host.SetRoot(root);

        Assert.Equal(800f, root.Width.Value);
        Assert.Equal(450f, root.Height.Value);
    }

    [Fact]
    public void AScaleChangeRelaysOutTheRoot()
    {
        var (host, _, scale) = Host(uiScale: 1f);
        var root = new ContainerView();
        host.SetRoot(root);

        scale.Value = 2f;
        host.SyncScale();

        Assert.Equal(800, host.Canvas.Width);
        Assert.Equal(800f, root.Width.Value);
        Assert.Equal(2f, host.Canvas.DpiScale);
    }

    [Fact]
    public void DraggingOntoADisplayWithDifferentScalingRescalesTheWindow()
    {
        var (host, window, _) = Host(uiScale: 1f);
        var root = new ContainerView();
        host.SetRoot(root);

        window.RaiseContentScaleChanged(2f);
        host.SyncScale();

        Assert.Equal(800, host.Canvas.Width);
        Assert.Equal(800f, root.Width.Value);
    }

    // Text that names no size of its own is drawn at the size its font was registered at, which is a
    // logical size like any other — so it has to be re-baked when the scale moves, not left at the
    // pixel size it happened to be loaded with.
    [Theory]
    [InlineData(1.5f)]
    [InlineData(2f)]
    public void UnstyledTextKeepsItsLogicalSizeAcrossAScaleChange(float scale)
    {
        var canvas = new CaptureCanvas(_fonts, _font);
        var before = canvas.MeasureTextLineHeight(new TextStyle());

        canvas.UpdateDpiScale(scale);

        Assert.Equal(before, canvas.MeasureTextLineHeight(new TextStyle()), 1f);
    }

    [Fact]
    public void TheHostsStateIsWhatTheWindowsRead()
    {
        var state = new FakeUiScale(1f);
        var uiScale = (IUiScale)state;
        var seen = new List<float>();
        uiScale.Changed += seen.Add;

        state.Value = 1.25f;

        Assert.Equal(1.25f, uiScale.Value);
        Assert.Equal([1.25f], seen);
    }
}
