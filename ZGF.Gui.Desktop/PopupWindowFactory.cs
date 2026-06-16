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
        PopupWindowImpl popup;
        if (_pool.Count > 0)
        {
            popup = _pool[^1];
            _pool.RemoveAt(_pool.Count - 1);
        }
        else
        {
            popup = CreateNewPopup(request.MousePassThrough);
        }

        popup.MousePassThrough = request.MousePassThrough;

        // Build the content against THIS popup's context so its controllers register with
        // this popup's input system and its text measures against this popup's canvas.
        // Built views are pinned to the window they were built for.
        var root = request.BuildRoot(popup.Context);
        popup.SetRoot(root);

        var width = (int)MathF.Ceiling(root.MeasureWidth());
        var height = (int)MathF.Ceiling(root.MeasureHeight(width));
        var (preferred, flipped) = request.Place(width, height);
        var rect = ResolveRect(preferred, flipped);

        popup.Resize(rect.Width, rect.Height);
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

    private RectI ResolveRect(in RectI preferredRect, in RectI? flippedRect)
    {
        // Pick the target monitor from the anchor (the click point = the rect's
        // top-left), NOT the rect's center. The rect extends right/down by the
        // full menu size, so its center is offset from the click by half the
        // menu; on a multi-monitor setup that offset can push the center across
        // the midpoint between two monitor centers and select the neighbouring
        // monitor while the click — and plenty of room — are still on this one.
        var anchor = new PointI(preferredRect.X, preferredRect.Y);
        var (mx, my, mw, mh) = GetMonitorWorkArea(anchor);
        if (FitsInside(preferredRect, mx, my, mw, mh))
            return preferredRect;
        if (flippedRect is { } flipped && FitsInside(flipped, mx, my, mw, mh))
            return flipped;
        return Clamp(preferredRect, mx, my, mw, mh);
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

    private PopupWindowImpl CreateNewPopup(bool mousePassThrough)
    {
        // Created at a placeholder size; Acquire resizes to the measured content
        // after building the root.
        const int initialSize = 32;
        var window = _app.CreatePopupWindow(new PopupWindowOptions
        {
            WidthPoints = initialSize,
            HeightPoints = initialSize,
            OwnerWindow = _app.MainWindow,
            MousePassThrough = mousePassThrough,
        });

        _decorator.DecoratePopup(window.NativeHandle, mousePassThrough);

        var canvas = _backend.CreateCanvas(window, initialSize, initialSize, _mainCanvasForFontRegistry);

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
    private readonly GuiWindowHost _host;

    public event Action<PointI>? OutsideClick;
    public bool MousePassThrough { get; set; }

    public IWindow Window => _host.Window;
    public Context Context => _host.Context;
    public View? Root => _host.Root;
    public IPointerWindow PointerWindow => _host.Input;

    public PopupWindowImpl(
        IWindow window,
        RenderedCanvasBase canvas,
        DesktopInputSystem input,
        Context context,
        IGuiRenderBackend backend)
    {
        _host = new GuiWindowHost(window, canvas, input, context, sizeRootToWindow: false);
        backend.WireRenderLoop(window, canvas, _host.DrawContent, (0f, 0f, 0f, 0f));
    }

    public void SetRoot(View? root) => _host.SetRoot(root);

    public void Resize(int width, int height) => _host.HandleResize(width, height);

    public void RefreshDpiScale() => _host.RefreshDpiScale();

    public void RaiseOutsideClick(PointI screen) => OutsideClick?.Invoke(screen);

    /// <summary>
    /// Removes all <see cref="OutsideClick"/> subscribers. Called when the popup is returned
    /// to the factory's pool so a reused instance doesn't carry the previous menu's handler.
    /// </summary>
    public void ClearOutsideClickSubscribers() => OutsideClick = null;

    public void UpdateInput() => _host.Input.Update();

    public void ResetInput() => _host.Input.Reset();

    public void Dispose()
    {
        SetRoot(null);
        _host.DisposeCanvas();
        _host.Window.Dispose();
    }
}