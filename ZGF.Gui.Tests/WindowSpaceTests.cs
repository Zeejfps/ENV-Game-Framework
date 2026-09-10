using ZGF.Geometry;
using ZGF.Gui.Desktop;

namespace ZGF.Gui.Tests;

/// <summary>
/// The one converter between the four coordinate spaces. Everything else in the scale work is
/// expressed in terms of it, and the two ways this has been got wrong before — the Y flip applied
/// twice, and screen points mistaken for pixels on a Retina panel — are both here or nowhere.
/// </summary>
public sealed class WindowSpaceTests
{
    private const float Tolerance = 1f;

    // A Windows-shaped window: the OS hands out sizes in pixels, so the framebuffer is the window.
    private static WindowSpace Flat(float scale, int width = 1600, int height = 900) =>
        WindowSpace.Of(new FakeWindow(width, height), scale);

    // A Retina-shaped window: 800 points wide against a 1600px framebuffer.
    private static WindowSpace Retina(float scale) =>
        WindowSpace.Of(new FakeWindow(800, 600, backingRatio: 2f), scale);

    [Theory]
    [InlineData(1f, 1600, 900)]
    [InlineData(1.5f, 1067, 600)]
    [InlineData(2f, 800, 450)]
    public void TheCanvasIsTheFramebufferDividedByTheScale(float scale, int width, int height)
    {
        var space = Flat(scale);

        Assert.Equal(width, space.CanvasSize.Width);
        Assert.Equal(height, space.CanvasSize.Height);
    }

    // The macOS regression guard. A Retina window is 800 points against 1600 pixels and reports a
    // content scale of 2; dividing the *points* by that scale would halve the UI on every Mac.
    [Fact]
    public void ARetinaWindowAtScaleTwoLaysOutAtItsPointSize()
    {
        var space = Retina(2f);

        Assert.Equal(800, space.CanvasSize.Width);
        Assert.Equal(600, space.CanvasSize.Height);
        Assert.Equal(2f, space.BackingRatio);
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(1.5f)]
    [InlineData(2f)]
    public void ACanvasPointRoundTripsThroughTheDesktop(float scale)
    {
        var space = Flat(scale);
        var point = new CanvasPoint(321f, 654f);

        var back = space.ToCanvas(space.ToScreen(point));

        Assert.Equal(point.X, back.X, Tolerance);
        Assert.Equal(point.Y, back.Y, Tolerance);
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(1.5f)]
    [InlineData(2f)]
    public void ACanvasPointRoundTripsOnARetinaPanel(float scale)
    {
        var space = Retina(2f * scale);
        var point = new CanvasPoint(123f, 234f);

        var back = space.ToCanvas(space.ToScreen(point));

        Assert.Equal(point.X, back.X, Tolerance);
        Assert.Equal(point.Y, back.Y, Tolerance);
    }

    [Fact]
    public void AWindowOnAMonitorLeftOfThePrimaryStillRoundTrips()
    {
        var window = new FakeWindow(1600, 900);
        window.SetPosition(-1920, -140);
        var space = WindowSpace.Of(window, 1.5f);
        var point = new CanvasPoint(200f, 300f);

        var screen = space.ToScreen(point);
        var back = space.ToCanvas(screen);

        Assert.True(screen.X < 0);
        Assert.Equal(point.X, back.X, Tolerance);
        Assert.Equal(point.Y, back.Y, Tolerance);
    }

    // Once per direction, and only there: a second flip anywhere would put the point back where it
    // started, and this is the one assertion that catches that.
    [Fact]
    public void TheYAxisIsFlippedExactlyOnceInEachDirection()
    {
        var space = Flat(1f, 1600, 900);

        Assert.Equal(900, space.ToScreen(new CanvasPoint(0f, 0f)).Y);
        Assert.Equal(0, space.ToScreen(new CanvasPoint(0f, 900f)).Y);
        Assert.Equal(900f, space.ToCanvas(new WindowPoint(0f, 0f)).Y, Tolerance);
        Assert.Equal(0f, space.ToCanvas(new WindowPoint(0f, 900f)).Y, Tolerance);
    }

    [Fact]
    public void AWindowPointAndTheScreenPointAboveItAgree()
    {
        var window = new FakeWindow(1600, 900);
        window.SetPosition(400, 250);
        var space = WindowSpace.Of(window, 1.5f);

        var fromWindow = space.ToCanvas(new WindowPoint(600f, 300f));
        var fromScreen = space.ToCanvas(new ScreenPoint(400 + 600, 250 + 300));

        Assert.Equal(fromWindow.X, fromScreen.X, Tolerance);
        Assert.Equal(fromWindow.Y, fromScreen.Y, Tolerance);
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(1.5f)]
    [InlineData(2f)]
    public void ASizeRoundTripsBetweenTheCanvasAndTheDesktop(float scale)
    {
        var space = Flat(scale);
        var size = new CanvasSize(200f, 300f);

        var back = space.ToCanvas(space.ToScreen(size));

        Assert.Equal(size.Width, back.Width, Tolerance);
        Assert.Equal(size.Height, back.Height, Tolerance);
    }

    [Fact]
    public void OnARetinaPanelAScreenPointIsAlreadyALogicalPoint()
    {
        var space = Retina(2f);

        Assert.Equal(new ScreenSize(200, 300), space.ToScreen(new CanvasSize(200f, 300f)));
    }

    [Fact]
    public void ATinyWindowStillHasANonZeroCanvas()
    {
        var space = WindowSpace.Of(new FakeWindow(1, 1), 2f);

        Assert.True(space.CanvasSize.Width >= 1f);
        Assert.True(space.CanvasSize.Height >= 1f);
    }
}
