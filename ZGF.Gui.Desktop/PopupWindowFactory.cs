using ZGF.Desktop;
using ZGF.Fonts;
using ZGF.Geometry;
using ZGF.Gui.Desktop.Input;

namespace ZGF.Gui.Desktop;

public sealed class PopupWindowFactory : IPopupWindowFactory
{
    private const int SoftCap = 16;

    private readonly IWindowedApp _app;
    private readonly FreeTypeFontBackend _fonts;
    private readonly FontHandle _defaultFont;
    private readonly IGuiRenderBackend _backend;
    private readonly IPopupNativeDecorator _decorator;
    private readonly Context _mainContext;
    private readonly PointerOwnershipArbiter _arbiter;
    private readonly RenderedCanvasBase? _mainCanvasForFontRegistry;

    private readonly List<PopupWindowImpl> _activePopups = new();
    private readonly List<PopupWindowImpl> _pool = new();
    private PopupWindowImpl? _captureHolder;

    internal PopupWindowFactory(
        IWindowedApp app,
        FreeTypeFontBackend fonts,
        FontHandle defaultFont,
        IGuiRenderBackend backend,
        IPopupNativeDecorator decorator,
        Context mainContext,
        PointerOwnershipArbiter arbiter,
        RenderedCanvasBase? mainCanvasForFontRegistry = null)
    {
        _app = app;
        _fonts = fonts;
        _defaultFont = defaultFont;
        _backend = backend;
        _decorator = decorator;
        _mainContext = mainContext;
        _arbiter = arbiter;
        _mainCanvasForFontRegistry = mainCanvasForFontRegistry;
    }

    public IPopupWindow Acquire(in PopupRequest request)
    {
        var rect = ResolveRect(request);

        PopupWindowImpl popup;
        if (_pool.Count > 0)
        {
            popup = _pool[^1];
            _pool.RemoveAt(_pool.Count - 1);
            popup.Resize(rect.Width, rect.Height);
        }
        else
        {
            popup = CreateNewPopup(rect.Width, rect.Height, request.MousePassThrough);
        }

        popup.MousePassThrough = request.MousePassThrough;
        popup.SetRoot(request.Root);
        popup.Window.SetSize(rect.Width, rect.Height);
        popup.Window.SetPosition(rect.X, rect.Y);

        // After positioning, sync the canvas DPI to the monitor the popup is
        // now on. Pooled popups may have been created at a different DPI than
        // the current monitor — without this, text on the new monitor renders
        // blurry (atlas baked too small) or chunky (baked too large).
        popup.RefreshDpiScale();

        // Synchronous render before show so the first paint isn't a flash.
        popup.Window.MakeContextCurrent();
        popup.Window.RenderNow();

        popup.Window.Show();
        _activePopups.Add(popup);

        // Capturing popups (menus) register as modal so the arbiter denies the main
        // window pointer ownership while they're open; passthrough popups (tooltips)
        // are click-through and never own the pointer, so they stay unregistered and
        // keep their independent hover behavior. A submenu registers after its parent,
        // placing it above the parent in z-order.
        _arbiter.Register(popup.PointerWindow, isModal: !request.MousePassThrough);

        if (!request.MousePassThrough)
        {
            if (_captureHolder == null)
            {
                _decorator.BeginCapture(popup.Window.NativeHandle, popup.RaiseOutsideClick);
                _captureHolder = popup;
            }
            else
            {
                _decorator.TransferCapture(_captureHolder.Window.NativeHandle, popup.Window.NativeHandle, popup.RaiseOutsideClick);
                _captureHolder = popup;
            }
        }

        // Build this popup's hover path now, synchronously, while it is the topmost
        // window under the cursor. A menu that opens directly under the pointer is then
        // live on the very first press — closing the "menu shows but eats the first
        // click" race where the path was only built a frame later, on the next tick.
        popup.UpdateInput();
        return popup;
    }

    public void Release(IPopupWindow popup)
    {
        if (popup is not PopupWindowImpl impl) return;
        if (!_activePopups.Remove(impl)) return;

        _arbiter.Unregister(impl.PointerWindow);

        if (_captureHolder == impl)
        {
            // Transfer capture back to the topmost remaining popup, if any.
            var newHolder = _activePopups.LastOrDefault(p => !p.MousePassThrough);
            if (newHolder != null)
            {
                _decorator.TransferCapture(impl.Window.NativeHandle, newHolder.Window.NativeHandle, newHolder.RaiseOutsideClick);
                _captureHolder = newHolder;
            }
            else
            {
                _decorator.EndCapture(impl.Window.NativeHandle);
                _captureHolder = null;
            }
        }

        impl.Window.Hide();
        impl.SetRoot(null);
        // Drop any OutsideClick subscribers the caller wired up (e.g. ContextMenuManager
        // does `popup.OutsideClick += HandleOutsideClick` on every ShowContextMenu). The
        // impl is pooled and reused, so without this the handler list grows by one on each
        // menu open — a slow leak plus redundant close dispatch on every outside click.
        impl.ClearOutsideClickSubscribers();
        // Drop any focus/hover the closing menu's views left on this popup's own
        // input system. SetRoot(null) unregisters per-controller, but a controller
        // that still holds focus would leave _focusedComponent latched — and since
        // this PopupWindowImpl (and its InputSystem) is pooled and reused, that
        // latch makes HasFocus true forever, short-circuiting DesktopInputSystem.Update
        // before hover/click dispatch. Result: the next pooled popup (and every one
        // after) opens dead. Reset clears the latch so each reuse starts clean.
        impl.ResetInput();

        if (_pool.Count >= SoftCap)
        {
            var evict = _pool[0];
            _pool.RemoveAt(0);
            evict.Dispose();
        }
        _pool.Add(impl);

        // When the last popup closes, restore OS key focus to the main window so
        // in-app dialog overlays (e.g. Create Tag) receive keyboard input without
        // requiring a manual click. On macOS, showing a borderless popup makes it
        // the key window; hiding it does not automatically return key status to
        // the main window. Only refocus once no popups remain — closing a submenu
        // while its parent menu is still open must not pull focus to main.
        if (_activePopups.Count == 0 && _app.MainWindow is { } main)
            main.Focus();
    }

    private RectI ResolveRect(in PopupRequest request)
    {
        // Pick the target monitor from the anchor (the click point = the rect's
        // top-left), NOT the rect's center. The rect extends right/down by the
        // full menu size, so its center is offset from the click by half the
        // menu; on a multi-monitor setup that offset can push the center across
        // the midpoint between two monitor centers and select the neighbouring
        // monitor while the click — and plenty of room — are still on this one.
        var anchor = new PointI(request.PreferredScreenRect.X, request.PreferredScreenRect.Y);
        var (mx, my, mw, mh) = GetMonitorWorkArea(anchor);
        if (FitsInside(request.PreferredScreenRect, mx, my, mw, mh))
            return request.PreferredScreenRect;
        if (request.FlippedScreenRect is { } flipped && FitsInside(flipped, mx, my, mw, mh))
            return flipped;
        return Clamp(request.PreferredScreenRect, mx, my, mw, mh);
    }

    private static bool FitsInside(RectI r, int mx, int my, int mw, int mh) =>
        r.X >= mx && r.Y >= my && r.X + r.Width <= mx + mw && r.Y + r.Height <= my + mh;

    private static RectI Clamp(RectI r, int mx, int my, int mw, int mh)
    {
        var x = Math.Min(Math.Max(r.X, mx), mx + mw - r.Width);
        var y = Math.Min(Math.Max(r.Y, my), my + mh - r.Height);
        return new RectI(x, y, r.Width, r.Height);
    }

    private (int x, int y, int w, int h) GetMonitorWorkArea(PointI anchor)
    {
        var monitors = _app.Monitors;
        if (monitors.Count == 0)
            return (0, 0, 1920, 1080);

        // Prefer the monitor whose work area actually contains the anchor point.
        // This is the monitor the user clicked on, regardless of menu size.
        foreach (var wa in monitors)
        {
            if (anchor.X >= wa.X && anchor.X < wa.X + wa.Width &&
                anchor.Y >= wa.Y && anchor.Y < wa.Y + wa.Height)
            {
                return (wa.X, wa.Y, wa.Width, wa.Height);
            }
        }

        // Fallback (anchor outside every work area — e.g. on a taskbar strip or
        // just off-screen): nearest monitor by distance from the anchor to the
        // work-area rect (0 when inside on an axis), not by centre distance.
        var best = monitors[0];
        var bestDist = long.MaxValue;
        foreach (var wa in monitors)
        {
            var dx = (long)Math.Max(0, Math.Max(wa.X - anchor.X, anchor.X - (wa.X + wa.Width - 1)));
            var dy = (long)Math.Max(0, Math.Max(wa.Y - anchor.Y, anchor.Y - (wa.Y + wa.Height - 1)));
            var d = dx * dx + dy * dy;
            if (d < bestDist) { bestDist = d; best = wa; }
        }
        return (best.X, best.Y, best.Width, best.Height);
    }

    private PopupWindowImpl CreateNewPopup(int width, int height, bool mousePassThrough)
    {
        var window = _app.CreatePopupWindow(new PopupWindowOptions
        {
            WidthPoints = width,
            HeightPoints = height,
            OwnerWindow = _app.MainWindow,
            MousePassThrough = mousePassThrough,
        });

        _decorator.DecoratePopup(window.NativeHandle, mousePassThrough);

        var canvas = _backend.CreateCanvas(window, width, height, _mainCanvasForFontRegistry);

        var input = new DesktopInputSystem(window, canvas, _arbiter);

        var popupContext = new Context(_mainContext);
        popupContext.Canvas = canvas;
        popupContext.AddService(input.InputSystem);
        // Register popup-specific IWindowCoordinates. Without this, callers that
        // resolve IWindowCoordinates from the popup's context (e.g. a submenu
        // anchored at a parent-menu-item's canvas position) would inherit the
        // main window's translator and compute screen anchors against the main
        // window's origin instead of this popup's.
        popupContext.AddService<IWindowCoordinates>(new WindowCoordinates(window, canvas));

        var impl = new PopupWindowImpl(window, canvas, input, popupContext, _backend);
        return impl;
    }

    public void UpdateActivePopupInput()
    {
        for (var i = 0; i < _activePopups.Count; i++)
            _activePopups[i].UpdateInput();
    }

    public void Dispose()
    {
        foreach (var p in _activePopups) p.Dispose();
        _activePopups.Clear();
        foreach (var p in _pool) p.Dispose();
        _pool.Clear();
    }
}

internal sealed class PopupWindowImpl : IPopupWindow, IDisposable
{
    private readonly IWindow _window;
    private readonly RenderedCanvasBase _canvas;
    private readonly DesktopInputSystem _input;
    private readonly Context _context;
    private View? _root;

    public event Action<PointI>? OutsideClick;
    public bool MousePassThrough { get; set; }

    public IWindow Window => _window;
    public IPointerWindow PointerWindow => _input;

    public PopupWindowImpl(
        IWindow window,
        RenderedCanvasBase canvas,
        DesktopInputSystem input,
        Context context,
        IGuiRenderBackend backend)
    {
        _window = window;
        _canvas = canvas;
        _input = input;
        _context = context;

        _input.OnAnyInput = () => _window.RequestRedraw();
        backend.WireRenderLoop(_window, _canvas, DrawContent, (0f, 0f, 0f, 0f));
    }

    private void DrawContent()
    {
        if (_root != null)
        {
            _root.LayoutSelf();
            _root.DrawSelf();
        }
    }

    public void SetRoot(View? root)
    {
        if (_root != null)
        {
            _root.Context = null;
        }
        _root = root;
        if (root != null)
        {
            root.Context = _context;
        }
    }

    void IPopupWindow.SetRoot(View root) => SetRoot((View?)root);

    public void Resize(int width, int height)
    {
        _canvas.Resize(width, height);
    }

    public void RefreshDpiScale()
    {
        _canvas.UpdateDpiScale(_window.DpiScale);
    }

    public void RaiseOutsideClick(PointI screen) => OutsideClick?.Invoke(screen);

    /// <summary>
    /// Removes all <see cref="OutsideClick"/> subscribers. Called when the popup is returned
    /// to the factory's pool so a reused instance doesn't carry the previous menu's handler.
    /// </summary>
    public void ClearOutsideClickSubscribers() => OutsideClick = null;

    public void UpdateInput() => _input.Update();

    public void ResetInput() => _input.Reset();

    public void Dispose()
    {
        SetRoot(null);
        if (_canvas is IDisposable d) d.Dispose();
        _window.Dispose();
    }
}