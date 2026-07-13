using ZGF.Desktop;
using ZGF.Fonts;
using ZGF.Gui.Desktop.Input;

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
    private readonly IPopupNativeDecorator _decorator;
    private readonly Context _mainContext;
    private readonly PointerOwnershipArbiter _arbiter;
    private readonly ImeCoordinator _ime;
    private readonly RenderedCanvasBase? _mainCanvasForFontRegistry;

    private readonly List<SecondaryWindowImpl> _active = new();

    internal SecondaryWindowFactory(
        IWindowedApp app,
        FreeTypeFontBackend fonts,
        FontHandle defaultFont,
        IGuiRenderBackend backend,
        IPopupNativeDecorator decorator,
        Context mainContext,
        PointerOwnershipArbiter arbiter,
        ImeCoordinator ime,
        RenderedCanvasBase? mainCanvasForFontRegistry = null)
    {
        _app = app;
        _fonts = fonts;
        _defaultFont = defaultFont;
        _backend = backend;
        _decorator = decorator;
        _mainContext = mainContext;
        _arbiter = arbiter;
        _ime = ime;
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

        // Share the app's pointer arbiter so this window participates in pointer ownership. Without
        // it the main window (which is arbitrated) keeps believing it owns the pointer at screen
        // points that overlap this window, and its widgets hover through this one.
        var input = new DesktopInputSystem(window, canvas, _arbiter, _app, _ime);

        // This window composes for its own fields, and hosts the IME for a menu opened from it — a
        // review window's base-branch picker composes against this window, not the main one.
        _ime.Register(input);

        var context = new Context(_mainContext);
        context.Canvas = canvas;
        context.AddService(input.InputSystem);
        context.AddService<IWindowCoordinates>(new WindowCoordinates(window, canvas));

        var impl = new SecondaryWindowImpl(window, canvas, input, context, _backend, _arbiter, _ime);
        impl.SetRoot(request.BuildRoot(context));

        // A title-bar / border grab on this window is a non-client press GLFW never reports and that
        // changes no focus, so it's the case where an open menu anchored here would otherwise never
        // dismiss. Route those presses to the arbiter's outside-press dismissal.
        _decorator.WatchWindowNonClientPress(window.NativeHandle, _arbiter.NotifyNonClientPress);

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
                // Restore the native wndproc before the window is destroyed so the decorator's
                // subclass table doesn't retain a dead handle.
                _decorator.UnwatchWindow(w.Window.NativeHandle);
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

    /// <summary>The currently open secondary windows. Exposed for inspection (the MCP server
    /// projects these to surfaces).</summary>
    internal IReadOnlyList<SecondaryWindowImpl> Active => _active;

    public void Dispose()
    {
        foreach (var w in _active)
        {
            _decorator.UnwatchWindow(w.Window.NativeHandle);
            w.Dispose();
        }
        _active.Clear();
    }
}

internal sealed class SecondaryWindowImpl : ISecondaryWindow, IDisposable
{
    private readonly GuiWindowHost _host;
    private readonly PointerOwnershipArbiter _arbiter;
    private readonly ImeCoordinator _ime;
    private bool _disposed;

    public IWindow Window => _host.Window;
    internal DesktopInputSystem Input => _host.Input;
    internal View? Root => _host.Root;
    public bool CloseRequested { get; private set; }
    public event Action? Closed;

    public SecondaryWindowImpl(
        IWindow window,
        RenderedCanvasBase canvas,
        DesktopInputSystem input,
        Context context,
        IGuiRenderBackend backend,
        PointerOwnershipArbiter arbiter,
        ImeCoordinator ime)
    {
        _host = new GuiWindowHost(window, canvas, input, context, sizeRootToWindow: true);
        _arbiter = arbiter;
        _ime = ime;

        // Register as a non-modal participant. The arbiter orders by registration as a z-order
        // proxy, so re-register on focus to keep this window's slot matching its on-screen stacking:
        // whichever top-level window the user last raised must own the pointer over an overlap.
        _arbiter.Register(input, isModal: false);

        window.OnResize += HandleResize;
        window.OnFramebufferResize += HandleFramebufferResize;
        window.OnFocusChanged += HandleFocusChanged;
        // The native close button asks to close — defer the actual teardown to the next
        // factory Update() so we don't destroy the window from inside its GLFW callback.
        window.OnClose += () => CloseRequested = true;

        backend.WireRenderLoop(window, canvas, _host.DrawContent, (0f, 0f, 0f, 1f));
    }

    private void HandleFocusChanged(bool focused)
    {
        // Raised to the front ⇒ move to the top of the arbiter's order so it wins pointer
        // ownership over any window it overlaps.
        if (focused)
        {
            _arbiter.Register(_host.Input, isModal: false);
            return;
        }
        // Focus left this window: close any open menu (e.g. a base-branch dropdown anchored in
        // this window) if the whole app lost focus. The arbiter dismisses only when no arbitrated
        // window still holds focus, so switching to an owned menu popup doesn't self-close it.
        _arbiter.NotifyFocusChanged();
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

        _arbiter.Unregister(_host.Input);
        _ime.Unregister(_host.Input);
        _host.Window.OnResize -= HandleResize;
        _host.Window.OnFramebufferResize -= HandleFramebufferResize;
        _host.Window.OnFocusChanged -= HandleFocusChanged;
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
