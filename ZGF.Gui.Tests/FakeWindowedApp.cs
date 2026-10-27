using ZGF.Desktop;
using ZGF.Fonts;
using ZGF.Gui.Hosting;

namespace ZGF.Gui.Tests;

/// <summary>A windowed app whose windows are <see cref="FakeWindow"/>s and whose monitor layout the
/// test declares — enough of a desktop for the popup placement path to run against.</summary>
internal sealed class FakeWindowedApp : IWindowedApp
{
    private readonly List<IWindow> _windows = new();
    private readonly float _backingRatio;

    public FakeWindowedApp(IReadOnlyList<MonitorWorkArea> monitors, float backingRatio = 1f)
    {
        Monitors = monitors;
        _backingRatio = backingRatio;
        MainWindow = new FakeWindow(1600, 900, backingRatio);
        _windows.Add(MainWindow);
    }

    public IWindow MainWindow { get; }
    public IReadOnlyList<IWindow> Windows => _windows;
    public IReadOnlyList<MonitorWorkArea> Monitors { get; }
    public WindowSystem WindowSystem => WindowSystem.None;
    public bool IsForeground => true;
    public WindowOptions? LastWindowOptions { get; private set; }

#pragma warning disable CS0067
    public event Action<bool>? OnForegroundChanged;
    public event Action? OnTick;
#pragma warning restore CS0067

    public void Run() { }
    public void Wake() { }
    public void Quit() { }

    public IWindow CreatePopupWindow(in PopupWindowOptions options) =>
        Track(new FakeWindow(options.WidthPoints, options.HeightPoints, _backingRatio));

    public IWindow CreateWindow(in WindowOptions options)
    {
        LastWindowOptions = options;
        return Track(new FakeWindow(options.WidthPoints, options.HeightPoints, _backingRatio));
    }

    private IWindow Track(IWindow window)
    {
        _windows.Add(window);
        return window;
    }

    public void Dispose() { }
}

/// <summary>A render backend with no device: canvases stage but never upload, and every context and
/// present step is a no-op.</summary>
internal sealed class FakeRenderBackend : IGuiRenderBackend
{
    public FakeRenderBackend(IWindowedApp app, FreeTypeFontBackend fonts, FontHandle defaultFont)
    {
        App = app;
        Fonts = fonts;
        DefaultFont = defaultFont;
        MainCanvas = new CaptureCanvas(fonts, defaultFont, app.MainWindow.DpiScale);
    }

    public float LastClearAlpha { get; private set; }
    public IWindowedApp App { get; }
    public FreeTypeFontBackend Fonts { get; }
    public FontHandle DefaultFont { get; }
    public RenderedCanvasBase MainCanvas { get; }

    public RenderedCanvasBase CreateCanvas(IWindow window, int width, int height) =>
        new CaptureCanvas(Fonts, DefaultFont, window.DpiScale);

    public void WireRenderLoop(IWindow window, RenderedCanvasBase canvas, Action drawContent, (float R, float G, float B, float A) clearColor, Action? preDraw = null)
        => LastClearAlpha = clearColor.A;
    public void WirePresentLoop(IWindow window, RenderedCanvasBase canvas) { }
    public void OnFramebufferResize(int width, int height) { }
    public void RenderWindowNow(IWindow window) { }
    public void DisposeWindow(IWindow window, RenderedCanvasBase canvas) => window.Dispose();
    public void MakeMainContextCurrent() { }
    public void RequestScreenshot(string path, Action? onComplete = null) { }
    public void RegisterServices(Context context) { }
    public void Dispose() { }
}
