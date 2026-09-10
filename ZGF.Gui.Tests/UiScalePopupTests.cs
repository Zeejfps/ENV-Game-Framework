using ZGF.Desktop;
using ZGF.Fonts;
using ZGF.Geometry;
using ZGF.Gui.Desktop;
using ZGF.Gui.Desktop.Input;
using ZGF.Gui.Views;

namespace ZGF.Gui.Tests;

/// <summary>
/// Popup placement is the one place logical and screen sizes genuinely diverge: the content measures
/// in logical points, and everything it is then placed against — the monitor's work area, the size
/// the OS is handed — is screen coordinates.
/// </summary>
public sealed class UiScalePopupTests : IDisposable
{
    private const int ContentWidth = 200;
    private const int ContentHeight = 300;
    private static readonly MonitorWorkArea Primary = new(0, 0, 1000, 800);

    private static string FontPath => Path.Combine(AppContext.BaseDirectory, "Assets", "Inter-Regular.ttf");

    private readonly FreeTypeFontBackend _fonts = new();
    private readonly FontHandle _font;

    public UiScalePopupTests()
    {
        _font = _fonts.LoadFontFromFile(FontPath, 16);
    }

    public void Dispose() => _fonts.Dispose();

    private sealed class Fixture
    {
        public required PopupWindowFactory Factory { get; init; }
        public required FakeUiScale Scale { get; init; }
    }

    private Fixture NewFixture(
        float uiScale, float backingRatio = 1f, IReadOnlyList<MonitorWorkArea>? monitors = null)
    {
        // On a Retina panel the display's content scale and the backing ratio are the same number,
        // so a fixture that raises one has to raise the other or it describes no real machine.
        var app = new FakeWindowedApp(
            monitors ?? [Primary with { ContentScale = backingRatio }], backingRatio);
        var arbiter = new PointerOwnershipArbiter();
        var scale = new FakeUiScale(uiScale);
        var factory = new PopupWindowFactory(
            app, _fonts, _font, new FakeRenderBackend(_fonts, _font), new FakePopupDecorator(),
            new Context(), arbiter, new ImeCoordinator(arbiter), scale);
        return new Fixture { Factory = factory, Scale = scale };
    }

    // The same placement a context menu asks for: below the anchor, flipping above it when the
    // monitor has no room.
    private static IPopupWindow OpenMenu(PopupWindowFactory factory, ScreenPoint anchor) =>
        factory.Acquire(new PopupRequest
        {
            BuildRoot = _ => new ContainerView { Width = ContentWidth, Height = ContentHeight },
            Place = size =>
            {
                var below = new ScreenRect(anchor.X, anchor.Y, size.Width, size.Height);
                return (below, below with { Y = anchor.Y - size.Height });
            },
            MousePassThrough = false,
        });

    private static RenderedCanvasBase CanvasOf(IPopupWindow popup) =>
        (RenderedCanvasBase)popup.Context.Canvas!;

    [Theory]
    [InlineData(1f)]
    [InlineData(1.5f)]
    public void TheWindowIsSizedInScreenCoordinates(float uiScale)
    {
        var fixture = NewFixture(uiScale);

        var popup = OpenMenu(fixture.Factory, new ScreenPoint(10, 10));

        Assert.Equal((int)MathF.Ceiling(ContentWidth * uiScale), popup.Window.Width);
        Assert.Equal((int)MathF.Ceiling(ContentHeight * uiScale), popup.Window.Height);
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(1.5f)]
    public void TheCanvasKeepsTheContentsLogicalSize(float uiScale)
    {
        var fixture = NewFixture(uiScale);

        var popup = OpenMenu(fixture.Factory, new ScreenPoint(10, 10));

        Assert.Equal(ContentWidth, CanvasOf(popup).Width);
        Assert.Equal(ContentHeight, CanvasOf(popup).Height);
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(1.5f)]
    public void AMenuWithNoRoomBelowFlipsAtEveryScale(float uiScale)
    {
        var fixture = NewFixture(uiScale);
        // Low enough on the monitor that the menu overflows the work area at both scales.
        var anchor = new ScreenPoint(10, Primary.Height - 20);

        var popup = OpenMenu(fixture.Factory, anchor);

        popup.Window.GetPosition(out _, out var y);
        Assert.Equal(anchor.Y - popup.Window.Height, y);
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(1.5f)]
    public void AMenuWithRoomBelowStaysBelowAtEveryScale(float uiScale)
    {
        var fixture = NewFixture(uiScale);
        var anchor = new ScreenPoint(10, 20);

        var popup = OpenMenu(fixture.Factory, anchor);

        popup.Window.GetPosition(out var x, out var y);
        Assert.Equal(anchor.X, x);
        Assert.Equal(anchor.Y, y);
    }

    [Fact]
    public void AMenuTooTallForEitherSideIsClampedOntoTheMonitor()
    {
        // At 2.0 the 300-point menu is 600 screen pixels: anchored halfway down an 800-pixel work
        // area neither placement fits, so it is pushed up to sit against the bottom edge.
        var fixture = NewFixture(uiScale: 2f);

        var popup = OpenMenu(fixture.Factory, new ScreenPoint(10, 400));

        popup.Window.GetPosition(out _, out var y);
        Assert.Equal(Primary.Y + Primary.Height - popup.Window.Height, y);
    }

    [Fact]
    public void APooledPopupReacquiredAfterAScaleChangeGetsTheNewSize()
    {
        var fixture = NewFixture(uiScale: 1f);
        var first = OpenMenu(fixture.Factory, new ScreenPoint(10, 10));
        var firstWindow = first.Window;
        fixture.Factory.Release(first);

        fixture.Scale.Value = 2f;
        var second = OpenMenu(fixture.Factory, new ScreenPoint(10, 10));

        Assert.Same(firstWindow, second.Window);
        Assert.Equal(ContentWidth * 2, second.Window.Width);
        Assert.Equal(ContentWidth, CanvasOf(second).Width);
        Assert.Equal(2f, CanvasOf(second).DpiScale);
    }

    [Fact]
    public void OnARetinaPanelAScreenPointIsStillALogicalPoint()
    {
        // The framebuffer is twice the window, so the OS window size is the logical size — the extra
        // pixels are the panel's, not the user's.
        var fixture = NewFixture(uiScale: 1f, backingRatio: 2f);

        var popup = OpenMenu(fixture.Factory, new ScreenPoint(10, 10));

        Assert.Equal(ContentWidth, popup.Window.Width);
        Assert.Equal(ContentWidth, CanvasOf(popup).Width);
    }

    // The menu is sized for the display it lands on, not the one the window that opened it sits on.
    [Fact]
    public void AMenuOnASecondMonitorIsSizedForThatMonitorsScale()
    {
        var scaled = new MonitorWorkArea(1000, 0, 1000, 800, ContentScale: 2f);
        var fixture = NewFixture(uiScale: 1f, monitors: [Primary, scaled]);

        var onPrimary = OpenMenu(fixture.Factory, new ScreenPoint(10, 10));
        var onScaled = OpenMenu(fixture.Factory, new ScreenPoint(1010, 10));

        Assert.Equal(ContentWidth, onPrimary.Window.Width);
        Assert.Equal(ContentWidth * 2, onScaled.Window.Width);
    }
}
