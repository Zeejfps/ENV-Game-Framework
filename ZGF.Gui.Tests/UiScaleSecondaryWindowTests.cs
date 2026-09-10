using ZGF.Desktop;
using ZGF.Fonts;
using ZGF.Gui.Desktop;
using ZGF.Gui.Desktop.Input;
using ZGF.Gui.Views;

namespace ZGF.Gui.Tests;

/// <summary>
/// Secondary windows under a UI scale. Their size is remembered across launches, so the number the
/// OS reports and the number the content lays out in have to stay apart: persisting a logical size
/// would shrink the window a little further on every restart.
/// </summary>
public sealed class UiScaleSecondaryWindowTests : IDisposable
{
    private const int SavedWidth = 1100;
    private const int SavedHeight = 800;

    private static string FontPath => Path.Combine(AppContext.BaseDirectory, "Assets", "Inter-Regular.ttf");

    private readonly FreeTypeFontBackend _fonts = new();
    private readonly FontHandle _font;

    public UiScaleSecondaryWindowTests()
    {
        _font = _fonts.LoadFontFromFile(FontPath, 16);
    }

    public void Dispose() => _fonts.Dispose();

    private (SecondaryWindowFactory Factory, FakeUiScale Scale) NewFactory(float uiScale)
    {
        var app = new FakeWindowedApp([new MonitorWorkArea(0, 0, 2560, 1440)]);
        var arbiter = new PointerOwnershipArbiter();
        var scale = new FakeUiScale(uiScale);
        var factory = new SecondaryWindowFactory(
            app, _fonts, _font, new FakeRenderBackend(_fonts, _font), new FakePopupDecorator(),
            new Context(), arbiter, new ImeCoordinator(arbiter), scale);
        return (factory, scale);
    }

    private static ISecondaryWindow Open(SecondaryWindowFactory factory) =>
        factory.Open(new SecondaryWindowRequest
        {
            BuildRoot = _ => new ContainerView(),
            Title = "Review",
            Width = SavedWidth,
            Height = SavedHeight,
        });

    [Theory]
    [InlineData(1f, 1100)]
    [InlineData(1.5f, 733)]
    [InlineData(2f, 550)]
    public void TheSavedSizeIsTheWindowsAndTheScaledSizeIsTheCanvasSize(float uiScale, int expectedLogicalWidth)
    {
        var (factory, _) = NewFactory(uiScale);

        var window = Open(factory);

        Assert.Equal(SavedWidth, window.Window.Width);
        Assert.Equal(expectedLogicalWidth, CanvasOf(factory).Width);
    }

    [Fact]
    public void AScaleChangeReachesAWindowThatIsAlreadyOpen()
    {
        var (factory, scale) = NewFactory(uiScale: 1f);
        var window = Open(factory);

        scale.Value = 2f;
        factory.SyncScale();

        Assert.Equal(SavedWidth / 2, CanvasOf(factory).Width);
        Assert.Equal(SavedWidth, window.Window.Width);
    }

    [Fact]
    public void WhatIsReportedForPersistenceStaysInScreenCoordinates()
    {
        var (factory, _) = NewFactory(uiScale: 1.5f);
        var window = Open(factory);
        var reported = new List<(int Width, int Height)>();
        window.Window.OnResize += (w, h) => reported.Add((w, h));

        window.Window.SetSize(900, 700);

        Assert.Equal([(900, 700)], reported);
        Assert.Equal(600, CanvasOf(factory).Width);
    }

    private static RenderedCanvasBase CanvasOf(SecondaryWindowFactory factory) => factory.Active[0].Canvas;
}
