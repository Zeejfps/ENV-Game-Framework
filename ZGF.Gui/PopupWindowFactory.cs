using GLFW;
using ZGF.Core;
using ZGF.Fonts;
using ZGF.Geometry;
using static GL46;
using static ZGF.Core.MacOs.Objc;

namespace ZGF.Gui;

public sealed class PopupWindowFactory : IPopupWindowFactory
{
    private const int SoftCap = 16;

    private readonly IApp _app;
    private readonly FreeTypeFontBackend _fonts;
    private readonly FontHandle _defaultFont;
    private readonly GlSharedResources? _glShared;
    private readonly MetalSharedResources? _metalShared;
    private readonly IPopupNativeDecorator _decorator;
    private readonly Context _mainContext;
    private readonly RenderedCanvasBase? _mainCanvasForFontRegistry;

    private readonly List<PopupWindowImpl> _activePopups = new();
    private readonly List<PopupWindowImpl> _pool = new();
    private PopupWindowImpl? _captureHolder;

    public PopupWindowFactory(
        IApp app,
        FreeTypeFontBackend fonts,
        FontHandle defaultFont,
        GlSharedResources? glShared,
        MetalSharedResources? metalShared,
        IPopupNativeDecorator decorator,
        Context mainContext,
        RenderedCanvasBase? mainCanvasForFontRegistry = null)
    {
        _app = app;
        _fonts = fonts;
        _defaultFont = defaultFont;
        _glShared = glShared;
        _metalShared = metalShared;
        _decorator = decorator;
        _mainContext = mainContext;
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

        if (!request.MousePassThrough)
        {
            if (_captureHolder == null)
            {
                _decorator.BeginCapture(popup.Window.WindowHandle, popup.RaiseOutsideClick);
                _captureHolder = popup;
            }
            else
            {
                _decorator.TransferCapture(_captureHolder.Window.WindowHandle, popup.Window.WindowHandle, popup.RaiseOutsideClick);
                _captureHolder = popup;
            }
        }

        return popup;
    }

    public void Release(IPopupWindow popup)
    {
        if (popup is not PopupWindowImpl impl) return;
        if (!_activePopups.Remove(impl)) return;

        if (_captureHolder == impl)
        {
            // Transfer capture back to the topmost remaining popup, if any.
            var newHolder = _activePopups.LastOrDefault(p => !p.MousePassThrough);
            if (newHolder != null)
            {
                _decorator.TransferCapture(impl.Window.WindowHandle, newHolder.Window.WindowHandle, newHolder.RaiseOutsideClick);
                _captureHolder = newHolder;
            }
            else
            {
                _decorator.EndCapture(impl.Window.WindowHandle);
                _captureHolder = null;
            }
        }

        impl.Window.Hide();
        impl.SetRoot(null);

        if (_pool.Count >= SoftCap)
        {
            var evict = _pool[0];
            _pool.RemoveAt(0);
            evict.Dispose();
        }
        _pool.Add(impl);
    }

    private RectI ResolveRect(in PopupRequest request)
    {
        var (mx, my, mw, mh) = GetMonitorWorkArea(request.PreferredScreenRect);
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

    private static (int x, int y, int w, int h) GetMonitorWorkArea(RectI screenRect)
    {
        var centerX = screenRect.X + screenRect.Width / 2;
        var centerY = screenRect.Y + screenRect.Height / 2;
        var monitors = Glfw.Monitors;
        if (monitors.Length == 0)
            return (0, 0, 1920, 1080);

        var best = monitors[0].WorkArea;
        var bestDist = long.MaxValue;
        foreach (var m in monitors)
        {
            var wa = m.WorkArea;
            var cmx = wa.X + wa.Width / 2;
            var cmy = wa.Y + wa.Height / 2;
            var dx = (long)(cmx - centerX);
            var dy = (long)(cmy - centerY);
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

        _decorator.DecoratePopup(window.WindowHandle, mousePassThrough);

        RenderedCanvasBase canvas;

        if (_glShared != null)
        {
            window.MakeContextCurrent();
            canvas = new OpenGlRenderedCanvas(width, height, _fonts, _defaultFont, _glShared, window.DpiScale);
            if (_mainCanvasForFontRegistry != null)
                canvas.CopyFontsFrom(_mainCanvasForFontRegistry);
        }
        else if (_metalShared != null)
        {
            canvas = new MetalRenderedCanvas(width, height, _fonts, _defaultFont, _metalShared, window.DpiScale);
            if (_mainCanvasForFontRegistry != null)
                canvas.CopyFontsFrom(_mainCanvasForFontRegistry);
            // Metal popup rendering routes through MetalWindow.Layer + a per-frame
            // command buffer. The closure is assembled in PopupWindowImpl.RenderFrame.
        }
        else
        {
            throw new InvalidOperationException("Either glShared or metalShared must be non-null.");
        }

        var input = new GlfwInputSystem(window.WindowHandle, canvas);

        var popupContext = new Context(_mainContext);
        popupContext.Canvas = canvas;
        popupContext.AddService(input.InputSystem);
        // Register popup-specific IWindowCoordinates. Without this, callers that
        // resolve IWindowCoordinates from the popup's context (e.g. a submenu
        // anchored at a parent-menu-item's canvas position) would inherit the
        // main window's translator and compute screen anchors against the main
        // window's origin instead of this popup's.
        popupContext.AddService<IWindowCoordinates>(new WindowCoordinates(window.WindowHandle, canvas));

        var impl = new PopupWindowImpl(window, canvas, input, popupContext, _glShared, _metalShared);
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
    private readonly GlfwInputSystem _input;
    private readonly Context _context;
    private readonly GlSharedResources? _glShared;
    private readonly MetalSharedResources? _metalShared;
    private View? _root;

    public event Action<PointI>? OutsideClick;
    public bool MousePassThrough { get; set; }

    public IWindow Window => _window;

    public PopupWindowImpl(
        IWindow window,
        RenderedCanvasBase canvas,
        GlfwInputSystem input,
        Context context,
        GlSharedResources? glShared,
        MetalSharedResources? metalShared)
    {
        _window = window;
        _canvas = canvas;
        _input = input;
        _context = context;
        _glShared = glShared;
        _metalShared = metalShared;

        _input.OnAnyInput = () => _window.RequestRedraw();
        WireRenderFrame();
    }

    private void WireRenderFrame()
    {
        if (_window is ZGF.Core.OpenGlWindow ogw && _glShared != null)
        {
            ogw.RenderFrame = () =>
            {
                glClearColor(0f, 0f, 0f, 0f);
                glClear(GL_COLOR_BUFFER_BIT);
                _canvas.BeginFrame();
                if (_root != null)
                {
                    _root.LayoutSelf();
                    _root.DrawSelf();
                }
                _canvas.EndFrame();
            };
        }
        else if (_window is ZGF.Core.MetalWindow mw && _metalShared != null)
        {
            var nextDrawableSel = Sel("nextDrawable");
            var commandBufferSel = Sel("commandBuffer");
            var renderCommandEncoderSel = Sel("renderCommandEncoderWithDescriptor:");
            var endEncodingSel = Sel("endEncoding");
            var presentDrawableSel = Sel("presentDrawable:");
            var commitSel = Sel("commit");
            var textureSel = Sel("texture");
            mw.RenderFrame = () =>
            {
                var device = _metalShared.Device;
                // The metal app owns the command queue; pull from the canvas/shared.
                var queue = MetalQueueAccessor.Get(_metalShared);
                var drawable = msg_IntPtr(mw.Layer, nextDrawableSel);
                if (drawable == IntPtr.Zero) return;
                var commandBuffer = msg_IntPtr(queue, commandBufferSel);
                var descClass = Class("MTLRenderPassDescriptor");
                var desc = msg_IntPtr(descClass, Sel("renderPassDescriptor"));
                Retain(desc);
                var attachments = msg_IntPtr(desc, Sel("colorAttachments"));
                var color0 = msg_IntPtr_NUInt_NUInt(attachments, Sel("objectAtIndexedSubscript:"), 0, 0);
                msg_Void_IntPtr(color0, Sel("setTexture:"), msg_IntPtr(drawable, textureSel));
                msg_Void_UInt(color0, Sel("setLoadAction:"), 2);
                msg_Void_UInt(color0, Sel("setStoreAction:"), 1);

                var encoder = msg_IntPtr(commandBuffer, renderCommandEncoderSel, desc);
                _canvas.BeginFrame();
                if (_root != null)
                {
                    _root.LayoutSelf();
                    _root.DrawSelf();
                }
                ((MetalRenderedCanvas)_canvas).EndFrame(encoder, commandBuffer);
                msg_Void(encoder, endEncodingSel);
                msg_Void_IntPtr(commandBuffer, presentDrawableSel, drawable);
                msg_Void(commandBuffer, commitSel);
                Release(desc);
            };
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

    public void UpdateInput() => _input.Update();

    public void Dispose()
    {
        SetRoot(null);
        if (_canvas is IDisposable d) d.Dispose();
        _window.Dispose();
    }
}

// MetalSharedResources doesn't currently expose the command queue (which is held
// by MetalApp). Pull from there at runtime — popups need the queue to submit per-frame.
internal static class MetalQueueAccessor
{
    public static IntPtr Get(MetalSharedResources shared)
    {
        // Reach into the MetalApp for the queue. PopupWindowFactory is only
        // constructed by GuiApp, which knows the IApp; this helper is a stand-in
        // for "give me the queue" without polluting MetalSharedResources with app state.
        // We store the queue on the shared object via a side-channel field.
        return shared.CommandQueue;
    }
}
