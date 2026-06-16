using ZGF.Desktop;
using ZGF.Fonts;

namespace ZGF.Gui.Desktop;

/// <summary>
/// Creates decorated, resizable secondary windows (see <see cref="ISecondaryWindowFactory"/>).
/// Mirrors the canvas/input/context/render wiring of <see cref="PopupWindowFactory"/>, but the
/// windows are persistent (not pooled), have no capture/outside-click behavior, and handle
/// their own resize and native-close lifecycle.
/// </summary>
public sealed class SecondaryWindowFactory : ISecondaryWindowFactory
{
    private readonly IWindowedApp _app;
    private readonly FreeTypeFontBackend _fonts;
    private readonly FontHandle _defaultFont;
    private readonly IGuiRenderBackend _backend;
    private readonly Context _mainContext;
    private readonly RenderedCanvasBase? _mainCanvasForFontRegistry;

    private readonly List<SecondaryWindowImpl> _active = new();

    internal SecondaryWindowFactory(
        IWindowedApp app,
        FreeTypeFontBackend fonts,
        FontHandle defaultFont,
        IGuiRenderBackend backend,
        Context mainContext,
        RenderedCanvasBase? mainCanvasForFontRegistry = null)
    {
        _app = app;
        _fonts = fonts;
        _defaultFont = defaultFont;
        _backend = backend;
        _mainContext = mainContext;
        _mainCanvasForFontRegistry = mainCanvasForFontRegistry;
    }

    public ISecondaryWindow Open(in SecondaryWindowRequest request)
    {
        var window = _app.CreateWindow(new WindowOptions
        {
            WidthPoints = request.Width,
            HeightPoints = request.Height,
            Title = request.Title,
        });

        var canvas = _backend.CreateCanvas(window, request.Width, request.Height, _mainCanvasForFontRegistry);

        var input = new DesktopInputSystem(window, canvas);

        var context = new Context(_mainContext);
        context.Canvas = canvas;
        context.AddService(input.InputSystem);
        context.AddService<IWindowCoordinates>(new WindowCoordinates(window, canvas));

        var impl = new SecondaryWindowImpl(window, canvas, input, context, _backend);
        impl.SetRoot(request.BuildRoot(context));

        // Paint once before showing so the first frame isn't a flash of an empty window.
        window.MakeContextCurrent();
        window.RenderNow();
        window.Show();

        _active.Add(impl);
        return impl;
    }

    /// <summary>
    /// Ticks each window's input and disposes any that requested close. Called once per app
    /// tick (deferring disposal out of the GLFW close callback that set the flag).
    /// </summary>
    public void Update()
    {
        for (var i = _active.Count - 1; i >= 0; i--)
        {
            var w = _active[i];
            if (w.CloseRequested)
            {
                _active.RemoveAt(i);
                w.Dispose();
                // w.Dispose() left no GL context current (it destroyed its own window after
                // deleting its objects under its own context). Restore the main context so the
                // run loop's next GL calls — and any GL work between now and the next per-window
                // MakeContextCurrent — target a valid context.
                _app.MakeMainContextCurrent();
            }
            else
            {
                w.UpdateInput();
            }
        }
    }

    public void Dispose()
    {
        foreach (var w in _active) w.Dispose();
        _active.Clear();
    }
}

internal sealed class SecondaryWindowImpl : ISecondaryWindow, IDisposable
{
    private readonly GuiWindowHost _host;
    private bool _disposed;

    public IWindow Window => _host.Window;
    public bool CloseRequested { get; private set; }
    public event Action? Closed;

    public SecondaryWindowImpl(
        IWindow window,
        RenderedCanvasBase canvas,
        DesktopInputSystem input,
        Context context,
        IGuiRenderBackend backend)
    {
        _host = new GuiWindowHost(window, canvas, input, context, sizeRootToWindow: true);

        window.OnResize += HandleResize;
        window.OnFramebufferResize += HandleFramebufferResize;
        // The native close button asks to close — defer the actual teardown to the next
        // factory Update() so we don't destroy the window from inside its GLFW callback.
        window.OnClose += () => CloseRequested = true;

        backend.WireRenderLoop(window, canvas, _host.DrawContent, (0f, 0f, 0f, 1f));
    }

    public void SetRoot(View? root) => _host.SetRoot(root);

    public void UpdateInput() => _host.Input.Update();

    public void RequestRedraw() => _host.Window.RequestRedraw();

    public void Close() => CloseRequested = true;

    private void HandleResize(int width, int height)
    {
        _host.HandleResize(width, height);
        // Repaint synchronously so a live drag-resize doesn't show stretched/stale content.
        _host.Window.MakeContextCurrent();
        _host.Window.RenderNow();
    }

    private void HandleFramebufferResize(int width, int height)
    {
        // Keep the atlas/viewport DPI in sync when the window moves between monitors of
        // different scale. The canvas recomputes its glViewport from Width*DpiScale each frame.
        _host.RefreshDpiScale();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _host.Window.OnResize -= HandleResize;
        _host.Window.OnFramebufferResize -= HandleFramebufferResize;
        SetRoot(null);
        // VAOs are per-context (not shared across the GL share group). Make THIS window's
        // context current before deleting the canvas's objects, otherwise glDeleteVertexArrays
        // runs against whatever context is current (often the main window) and destroys that
        // context's same-named VAOs — corrupting the main window's rendering.
        _host.Window.MakeContextCurrent();
        _host.DisposeCanvas();
        _host.Window.Dispose();
        Closed?.Invoke();
    }
}
