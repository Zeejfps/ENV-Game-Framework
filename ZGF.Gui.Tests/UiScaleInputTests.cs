using ZGF.Desktop;
using ZGF.Fonts;
using ZGF.Gui.Desktop;

namespace ZGF.Gui.Tests;

/// <summary>
/// Where the pointer lands once a logical point stops being a device pixel. The cursor arrives in the
/// window's own coordinates and is divided into logical points exactly once — the window-rect tests
/// beside that conversion stay in the window's coordinates, and dividing them too would move the
/// pointer twice.
/// </summary>
public sealed class UiScaleInputTests : IDisposable
{
    private const int WindowWidth = 1600;
    private const int WindowHeight = 900;
    private const float Tolerance = 1f;

    private static string FontPath => Path.Combine(AppContext.BaseDirectory, "Assets", "Inter-Regular.ttf");

    private readonly FreeTypeFontBackend _fonts = new();
    private readonly FontHandle _font;

    public UiScaleInputTests()
    {
        _font = _fonts.LoadFontFromFile(FontPath, 16);
    }

    public void Dispose() => _fonts.Dispose();

    private (DesktopInputSystem Input, FakeWindow Window) Wired(float uiScale)
    {
        var window = new FakeWindow(WindowWidth, WindowHeight);
        var canvas = new CaptureCanvas(_fonts, _font);
        var uiScaleSource = new FakeUiScale(uiScale);
        var input = new DesktopInputSystem(window, uiScaleSource);
        _ = new GuiWindowHost(window, canvas, input, new Context(), uiScaleSource, sizeRootToWindow: true);
        return (input, window);
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(1.25f)]
    [InlineData(1.5f)]
    [InlineData(2f)]
    public void AClickLandsAtTheCursorDividedByTheScale(float uiScale)
    {
        var (input, window) = Wired(uiScale);
        window.SetCursorPosition(800, 450);

        window.RaiseMouseButton(0, InputAction.Press);

        Assert.Equal(800 / uiScale, input.Mouse.Point.X, Tolerance);
        // Canvas Y grows upward from the window's bottom edge.
        Assert.Equal((WindowHeight - 450) / uiScale, input.Mouse.Point.Y, Tolerance);
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(1.5f)]
    [InlineData(2f)]
    public void DragDeltasScaleWithThePointer(float uiScale)
    {
        var (input, window) = Wired(uiScale);

        window.SetCursorPosition(400, 400);
        input.Update();
        var start = input.Mouse.Point;

        window.SetCursorPosition(500, 400);
        input.Update();
        var end = input.Mouse.Point;

        Assert.Equal(100 / uiScale, end.X - start.X, Tolerance);
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(1.5f)]
    [InlineData(2f)]
    public void TheHoverPollAndTheClickAgreeOnWhereThePointerIs(float uiScale)
    {
        var (input, window) = Wired(uiScale);
        window.SetCursorPosition(1234, 321);

        input.Update();
        var polled = input.Mouse.Point;
        window.RaiseMouseButton(0, InputAction.Press);
        var clicked = input.Mouse.Point;

        Assert.Equal(polled, clicked);
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(2f)]
    public void TheWindowRectTestStaysInTheWindowsOwnCoordinates(float uiScale)
    {
        var (input, window) = Wired(uiScale);

        // Just inside the far corner in window coordinates. Divided by the scale as well, this would
        // read as comfortably inside at 2.0 and the test would prove nothing; a cursor just *outside*
        // is what catches the double division.
        window.SetCursorPosition(WindowWidth - 1, WindowHeight - 1);
        Assert.True(input.IsCursorInsideWindow());

        window.SetCursorPosition(WindowWidth + 10, WindowHeight + 10);
        Assert.False(input.IsCursorInsideWindow());
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(2f)]
    public void APointerTakenByADriverSurvivesASmallPhysicalJitter(float uiScale)
    {
        var (input, window) = Wired(uiScale);
        window.SetCursorPosition(400, 400);

        input.BeginDrivingPointer();
        // Under the 4px resume threshold, measured in the window's coordinates like the cursor itself.
        window.SetCursorPosition(402, 400);
        input.Update();

        Assert.True(input.PointerDriven);

        window.SetCursorPosition(440, 400);
        input.Update();

        Assert.False(input.PointerDriven);
    }
}
