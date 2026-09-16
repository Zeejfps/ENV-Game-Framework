using ZGF.Desktop;
using ZGF.Fonts;
using ZGF.Gui.Desktop.Input;

namespace ZGF.Gui.Desktop;

/// <summary>
/// Creates persistent secondary windows (see <see cref="ISecondaryWindowFactory"/>).
/// Mirrors the canvas/input/context/render wiring of <see cref="PopupWindowFactory"/>, but the
/// windows are persistent (not pooled), have no capture/outside-click behavior, and handle
/// their own resize and native-close lifecycle.
/// </summary>
public sealed class SecondaryWindowFactory : ISecondaryWindowFactory
{
    private readonly IWindowedApp _app;
    private readonly IGuiRenderBackend _backend;
    private readonly IPopupNativeDecorator _decorator;
    private readonly Context _mainContext;
    private readonly PointerOwnershipArbiter _arbiter;
    private readonly ImeCoordinator _ime;
    private readonly IUiScale _uiScale;
    private readonly IWindowModality? _nativeModality;
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
        IUiScale uiScale,
        RenderedCanvasBase? mainCanvasForFontRegistry = null)
    {
        _app = app;
        _backend = backend;
        _decorator = decorator;
        _mainContext = mainContext;
        _arbiter = arbiter;
        _ime = ime;
        _uiScale = uiScale;
        _nativeModality = mainContext.Get<IWindowModality>();
        _mainCanvasForFontRegistry = mainCanvasForFontRegistry;
        _app.MainWindow.OnFocusChanged += HandleWindowFocusChanged;
    }

    private SecondaryWindowImpl? ModalWindow => _active.LastOrDefault(w => w.IsModal);

    private void SyncModality()
    {
        var modal = ModalWindow;
        _arbiter.DialogWindow = modal?.Input;
        _nativeModality?.SetInputEnabled(_app.MainWindow, modal == null);
        foreach (var window in _active)
            _nativeModality?.SetInputEnabled(window.Window, modal == null || ReferenceEquals(window, modal));
    }

    private void HandleWindowFocusChanged(bool focused)
    {
        if (focused && ModalWindow is { } modal && !modal.Window.IsFocused)
            modal.Window.Focus();
    }

    public ISecondaryWindow Open(in SecondaryWindowRequest request)
    {
        var width = request.Width;
        var height = request.Height;
        var x = request.X;
        var y = request.Y;
        if (request.CenterOnMainWindow)
        {
            var owner = _app.MainWindow;
            owner.GetPosition(out var ownerX, out var ownerY);
            var centerX = ownerX + owner.Width / 2;
            var centerY = ownerY + owner.Height / 2;
            var monitors = _app.Monitors;
            if (monitors.Count > 0)
            {
                var monitor = monitors.FirstOrDefault(m => centerX >= m.X && centerX < m.X + m.Width
                    && centerY >= m.Y && centerY < m.Y + m.Height, monitors[0]);
                width = Math.Clamp(width, 1, monitor.Width);
                height = Math.Clamp(height, 1, monitor.Height);
            }
            x = centerX - width / 2;
            y = centerY - height / 2;
        }
        var window = _app.CreateWindow(new WindowOptions
        {
            WidthPoints = width,
            HeightPoints = height,
            Title = request.Title,
            IsUndecorated = request.IsUndecorated,
        });

        var canvas = _backend.CreateCanvas(window, width, height, _mainCanvasForFontRegistry);

        // Share the app's pointer arbiter so this window participates in pointer ownership. Without
        // it the main window (which is arbitrated) keeps believing it owns the pointer at screen
        // points that overlap this window, and its widgets hover through this one.
        var input = new DesktopInputSystem(window, _uiScale, _arbiter, _app, _ime);

        // This window composes for its own fields, and hosts the IME for a menu opened from it — a
        // review window's base-branch picker composes against this window, not the main one.
        _ime.Register(input);

        var context = new Context(_mainContext);
        context.Canvas = canvas;
        context.AddService<IWindow>(window);
        context.AddService(input.InputSystem);
        context.AddService<IWindowCoordinates>(new WindowCoordinates(window, _uiScale));

        var impl = new SecondaryWindowImpl(window, canvas, input, context, _uiScale, _backend, _arbiter, _ime,
            request.IsUndecorated, request.IsModal);
        try
        {
            impl.SetRoot(request.BuildRoot(context));
        }
        catch
        {
            impl.Dispose();
            _backend.MakeMainContextCurrent();
            throw;
        }

        // A title-bar / border grab on this window is a non-client press GLFW never reports and that
        // changes no focus, so it's the case where an open menu anchored here would otherwise never
        // dismiss. Route those presses to the arbiter's outside-press dismissal.
        _decorator.WatchWindowNonClientPress(window.NativeHandle, _arbiter.NotifyNonClientPress);

        if (x is { } screenX && y is { } screenY)
        {
            var (px, py) = WindowPlacement.Compute(_app.Monitors, width, height, screenX, screenY);
            window.SetPosition(px, py);
        }

        // Paint once before showing so the first frame isn't a flash of an empty window.
        _backend.RenderWindowNow(window);
        if (request.IsModal)
        {
            _arbiter.NotifyNonClientPress(); // Dismiss any menu belonging to the previous window.
            _nativeModality?.SetOwner(window, ModalWindow?.Window ?? _app.MainWindow);
        }
        window.OnFocusChanged += HandleWindowFocusChanged;
        _active.Add(impl);
        SyncModality();
        window.Show();
        if (request.IsModal) window.Focus();
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
                w.Window.OnFocusChanged -= HandleWindowFocusChanged;
                // Enable the owner before destroying its dialog so the OS can activate it.
                SyncModality();
                // Restore the native wndproc before the window is destroyed so the decorator's
                // subclass table doesn't retain a dead handle.
                _decorator.UnwatchWindow(w.Window.NativeHandle);
                w.Dispose();
                // w.Dispose() left no GL context current (it destroyed its own window after
                // deleting its objects under its own context). Restore the main context so the
                // run loop's next GL calls — and any GL work between now and the next per-window
                // MakeContextCurrent — target a valid context.
                _backend.MakeMainContextCurrent();
                if (w.IsModal) (ModalWindow?.Window ?? _app.MainWindow).Focus();
            }
            else
            {
                w.UpdateInput();
            }
        }
    }

    /// <summary>Re-derives every open window's canvas scale and size — how a UI-scale change reaches
    /// windows the setting wasn't changed in.</summary>
    internal void SyncScale()
    {
        foreach (var w in _active)
            w.SyncScale();
    }

    /// <summary>The currently open secondary windows. Exposed for inspection (the MCP server
    /// projects these to surfaces).</summary>
    internal IReadOnlyList<SecondaryWindowImpl> Active => _active;

    public void Dispose()
    {
        _app.MainWindow.OnFocusChanged -= HandleWindowFocusChanged;
        _arbiter.DialogWindow = null;
        _nativeModality?.SetInputEnabled(_app.MainWindow, true);
        foreach (var w in _active) _nativeModality?.SetInputEnabled(w.Window, true);
        // Owned windows must be destroyed before their owners.
        for (var i = _active.Count - 1; i >= 0; i--)
        {
            var w = _active[i];
            w.Window.OnFocusChanged -= HandleWindowFocusChanged;
            _decorator.UnwatchWindow(w.Window.NativeHandle);
            w.Dispose();
        }
        _active.Clear();
    }
}

internal sealed class SecondaryWindowImpl : ISecondaryWindow, IDisposable
{
    private readonly GuiWindowHost _host;
    private readonly IGuiRenderBackend _backend;
    private readonly PointerOwnershipArbiter _arbiter;
    private readonly ImeCoordinator _ime;
    private bool _disposed;

    public IWindow Window => _host.Window;
    internal DesktopInputSystem Input => _host.Input;
    internal View? Root => _host.Root;
    internal RenderedCanvasBase Canvas => _host.Canvas;
    internal float Scale => _host.Space.Scale;
    public bool CloseRequested { get; private set; }
    internal bool IsModal { get; }
    public event Action? Closed;

    public SecondaryWindowImpl(
        IWindow window,
        RenderedCanvasBase canvas,
        DesktopInputSystem input,
        Context context,
        IUiScale uiScale,
        IGuiRenderBackend backend,
        PointerOwnershipArbiter arbiter,
        ImeCoordinator ime,
        bool transparent,
        bool isModal = false)
    {
        _host = new GuiWindowHost(window, canvas, input, context, uiScale, sizeRootToWindow: true);
        _backend = backend;
        _arbiter = arbiter;
        _ime = ime;
        IsModal = isModal;

        // Register as a non-modal participant. The arbiter orders by registration as a z-order
        // proxy, so re-register on focus to keep this window's slot matching its on-screen stacking:
        // whichever top-level window the user last raised must own the pointer over an overlap.
        _arbiter.Register(input, isModal: false);

        window.OnResize += HandleResize;
        window.OnFramebufferResize += HandleFramebufferResize;
        window.OnContentScaleChanged += HandleContentScaleChanged;
        window.OnFocusChanged += HandleFocusChanged;
        // The native close button asks to close — defer the actual teardown to the next
        // factory Update() so we don't destroy the window from inside its GLFW callback.
        window.OnClose += HandleClose;

        backend.WireRenderLoop(window, canvas, _host.DrawContent, (0f, 0f, 0f, transparent ? 0f : 1f));
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

    internal void SyncScale()
    {
        _host.SyncScale();
        _host.Window.RequestRedraw();
    }

    public void RequestRedraw() => _host.Window.RequestRedraw();

    public void Close() => CloseRequested = true;

    private void HandleClose()
    {
        if (_arbiter.IsBlockedByDialog(_host.Input)) _host.Window.CancelClose();
        else Close();
    }

    private void HandleResize(int width, int height)
    {
        _host.SyncScale();
        // Repaint synchronously so a live drag-resize doesn't show stretched/stale content.
        _backend.RenderWindowNow(_host.Window);
    }

    private void HandleFramebufferResize(int width, int height)
    {
        // Keep the atlas and the viewport in sync when the window moves between monitors of
        // different scale.
        _host.SyncScale();
    }

    private void HandleContentScaleChanged(float contentScale) => SyncScale();

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _arbiter.Unregister(_host.Input);
        _ime.Unregister(_host.Input);
        _host.Window.OnResize -= HandleResize;
        _host.Window.OnFramebufferResize -= HandleFramebufferResize;
        _host.Window.OnContentScaleChanged -= HandleContentScaleChanged;
        _host.Window.OnFocusChanged -= HandleFocusChanged;
        _host.Window.OnClose -= HandleClose;
        SetRoot(null);
        // VAOs are per-context (not shared across the GL share group). Make THIS window's
        // context current before deleting the canvas's objects, otherwise glDeleteVertexArrays
        // runs against whatever context is current (often the main window) and destroys that
        // context's same-named VAOs — corrupting the main window's rendering.
        _backend.MakeWindowContextCurrent(_host.Window);
        _host.DisposeCanvas();
        _host.Window.Dispose();
        Closed?.Invoke();
    }
}
