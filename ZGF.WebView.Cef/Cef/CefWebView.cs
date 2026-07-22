using System.Text;
using Xilium.CefGlue;
using ZGF.Gui;

namespace ZGF.WebView.Cef;

/// <summary>
/// One CEF browser rendered off-screen, exposed through ZGF's engine-neutral <see cref="IWebView"/>.
/// Because CEF runs with an external message pump, every browser callback (paint, title, cursor, bridge
/// message) fires on the UI thread during a pump — so the pixel buffer and events need no locking or
/// marshalling. Calls made before the browser finishes creating are queued and flushed in
/// <see cref="OnAfterCreated"/>.
/// </summary>
internal sealed class CefWebView : IWebView
{
    private readonly CefRuntimeHost _host;
    private readonly CefWebViewClient _client;
    private readonly List<Action> _deferred = new();

    private CefBrowser? _browser;
    private CefBrowserHost? _browserHost;
    private string _initialUrl = "about:blank";
    private bool _created;
    private bool _disposed;

    // Off-screen surface (UI thread only).
    private byte[] _pixels = [];
    private int _frameWidth;
    private int _frameHeight;
    private bool _frameDirty;

    // Logical (DIP) size + device scale CEF renders at; OnPaint returns logical*scale device pixels.
    private int _logicalWidth = 1;
    private int _logicalHeight = 1;
    private float _dpi = 1f;

    public CefWebView(CefRuntimeHost host)
    {
        _host = host;
        _client = new CefWebViewClient(this);
    }

    public event Action<string>? MessageReceived;
    public event Action<string>? TitleChanged;
    public event Action<string>? UrlChanged;
    public event Action<WebCursor>? CursorChanged;
    public event Action? FrameReady;

    // --- Creation (driven by CefRuntimeHost once CEF is initialised) -----------

    internal void CreateBrowser()
    {
        if (_created || _disposed)
            return;
        _created = true;

        var windowInfo = CefWindowInfo.Create();
        windowInfo.SetAsWindowless(IntPtr.Zero, transparent: false);
        windowInfo.WindowlessRenderingEnabled = true;

        var settings = new CefBrowserSettings
        {
            WindowlessFrameRate = 30,
            // Opaque background sidesteps CEF's premultiplied-alpha OnPaint buffer (plan decision 5).
            BackgroundColor = new CefColor(255, 255, 255, 255),
        };

        CefLog.Write($"CreateBrowser url={_initialUrl} logical={_logicalWidth}x{_logicalHeight}");
        CefBrowserHost.CreateBrowser(windowInfo, _client, settings, _initialUrl, null, null);
    }

    // --- Handler callbacks (all on the UI thread) -----------------------------

    internal void OnAfterCreated(CefBrowser browser)
    {
        CefLog.Write("OnAfterCreated");
        _browser = browser;
        _browserHost = browser.GetHost();
        _browserHost.WasResized();
        foreach (var action in _deferred)
            action();
        _deferred.Clear();
    }

    internal void OnBrowserClosed()
    {
        _browser = null;
        _browserHost = null;
    }

    // Auto-reloads to recover from a render-process crash. Bounded so a page that reliably kills its
    // renderer can't spin in reload→crash→reload; each user Navigate() resets the budget (see Navigate).
    private int _crashReloads;
    private const int MaxCrashReloads = 2;

    // The render process died (crash / OOM / killed) — the surface is frozen on its last frame, which is
    // the usual cause of a "web view stopped responding" report. Log the cause loudly, then reload once
    // to respawn the renderer (the browser host outlives it).
    internal void OnRenderProcessGone(CefTerminationStatus status)
    {
        var url = _browser?.GetMainFrame()?.Url ?? _initialUrl;
        if (_disposed || _browserHost == null)
        {
            CefLog.Error($"render process {Describe(status)} on {url}; view is disposed, not recovering");
            return;
        }
        if (_crashReloads++ >= MaxCrashReloads)
        {
            CefLog.Error($"render process {Describe(status)} on {url}; " +
                         $"reload budget ({MaxCrashReloads}) exhausted — leaving the page blank. " +
                         "This page repeatedly crashes the bundled Chromium 120 renderer.");
            return;
        }
        CefLog.Error($"render process {Describe(status)} on {url}; reloading (attempt {_crashReloads}/{MaxCrashReloads})");
        _browser?.Reload();
    }

    private static string Describe(CefTerminationStatus status) => status switch
    {
        CefTerminationStatus.ProcessCrashed => "CRASHED",
        CefTerminationStatus.WasKilled => "was KILLED (OS terminated it)",
        CefTerminationStatus.OutOfMemory => "ran OUT OF MEMORY",
        _ => status.ToString(),
    };

    private int _viewRectCount;

    internal void GetViewRect(out CefRectangle rect)
    {
        rect = new CefRectangle(0, 0, Math.Max(1, _logicalWidth), Math.Max(1, _logicalHeight));
        if (_viewRectCount++ < 3) CefLog.Write($"GetViewRect -> {rect.Width}x{rect.Height}");
    }

    internal bool GetScreenInfo(CefScreenInfo info)
    {
        info.DeviceScaleFactor = _dpi;
        info.Rectangle = new CefRectangle(0, 0, Math.Max(1, _logicalWidth), Math.Max(1, _logicalHeight));
        info.AvailableRectangle = info.Rectangle;
        return true;
    }

    private int _paintCount;

    internal unsafe void OnPaint(CefPaintElementType type, IntPtr buffer, int width, int height)
    {
        if (_paintCount++ < 5) CefLog.Write($"OnPaint #{_paintCount} type={type} {width}x{height}");
        if (type != CefPaintElementType.View || width <= 0 || height <= 0)
            return; // Popup element (e.g. <select> dropdown) compositing is deferred (plan decision 9).

        var needed = width * height * 4;
        if (_pixels.Length < needed)
            _pixels = new byte[needed];

        // CEF hands a top-left-origin BGRA buffer; swizzle to RGBA. CreateOrUpdateRgbaImage flips rows
        // to the GL/Metal convention internally, so the top-down copy draws right-side up.
        var src = (byte*)buffer;
        fixed (byte* dstPtr = _pixels)
        {
            var px = width * height;
            for (var i = 0; i < px; i++)
            {
                dstPtr[i * 4 + 0] = src[i * 4 + 2];
                dstPtr[i * 4 + 1] = src[i * 4 + 1];
                dstPtr[i * 4 + 2] = src[i * 4 + 0];
                dstPtr[i * 4 + 3] = src[i * 4 + 3];
            }
        }

        _frameWidth = width;
        _frameHeight = height;
        _frameDirty = true;
        FrameReady?.Invoke();
    }

    internal void RaiseTitle(string title) => TitleChanged?.Invoke(title);
    internal void RaiseUrl(string url) => UrlChanged?.Invoke(url);
    internal void RaiseMessage(string json) => MessageReceived?.Invoke(json);
    internal void RaiseCursor(CefCursorType type) => CursorChanged?.Invoke(MapCursor(type));

    // --- IWebView: surface -----------------------------------------------------

    public void Resize(int widthPx, int heightPx, float dpiScale)
    {
        _dpi = dpiScale <= 0f ? 1f : dpiScale;
        _logicalWidth = Math.Max(1, (int)MathF.Round(widthPx / _dpi));
        _logicalHeight = Math.Max(1, (int)MathF.Round(heightPx / _dpi));
        _browserHost?.WasResized();
    }

    public bool TryAcquireFrame(out int widthPx, out int heightPx, out ReadOnlySpan<byte> rgba)
    {
        if (!_frameDirty || _frameWidth <= 0 || _frameHeight <= 0)
        {
            widthPx = heightPx = 0;
            rgba = default;
            return false;
        }
        widthPx = _frameWidth;
        heightPx = _frameHeight;
        rgba = _pixels.AsSpan(0, _frameWidth * _frameHeight * 4);
        _frameDirty = false;
        return true;
    }

    // --- IWebView: navigation / scripting --------------------------------------

    public void Navigate(string url)
    {
        if (_disposed) return;
        _crashReloads = 0; // a fresh navigation is a fresh page — restore its crash-recovery budget.
        if (_browser != null)
            _browser.GetMainFrame().LoadUrl(url);
        else if (!_created)
            _initialUrl = url;
        else
            _deferred.Add(() => _browser!.GetMainFrame().LoadUrl(url));
    }

    public void LoadHtml(string html, string baseUrl = "about:blank")
    {
        var dataUrl = "data:text/html;charset=utf-8;base64," +
                      Convert.ToBase64String(Encoding.UTF8.GetBytes(html));
        Navigate(dataUrl);
    }

    public void ExecuteScript(string js) =>
        RunOrDefer(b => b.GetMainFrame().ExecuteJavaScript(js, string.Empty, 0));

    public void PostMessage(string json) =>
        ExecuteScript($"if(window.zgf&&window.zgf.onmessage){{window.zgf.onmessage({json});}}");

    public void GoBack() => RunOrDefer(b => b.GoBack());
    public void GoForward() => RunOrDefer(b => b.GoForward());
    public void Reload() => RunOrDefer(b => b.Reload());

    // --- IWebView: input -------------------------------------------------------

    public void SendMouseMove(float x, float y, WebInputModifiers modifiers) =>
        _browserHost?.SendMouseMoveEvent(MouseEvent(x, y, modifiers), mouseLeave: false);

    public void SendMouseLeave() =>
        _browserHost?.SendMouseMoveEvent(new CefMouseEvent(0, 0, CefEventFlags.None), mouseLeave: true);

    public void SendMouseButton(float x, float y, WebMouseButton button, bool pressed, int clickCount, WebInputModifiers modifiers) =>
        _browserHost?.SendMouseClickEvent(MouseEvent(x, y, modifiers), MapButton(button), mouseUp: !pressed, clickCount);

    public void SendMouseWheel(float x, float y, float deltaX, float deltaY, WebInputModifiers modifiers) =>
        _browserHost?.SendMouseWheelEvent(MouseEvent(x, y, modifiers), (int)deltaX, (int)deltaY);

    public void SendKey(WebKeyEventKind kind, int windowsKeyCode, int nativeKeyCode, char character, WebInputModifiers modifiers, bool isSystemKey)
    {
        if (_browserHost == null) return;
        _browserHost.SendKeyEvent(new CefKeyEvent
        {
            EventType = MapKeyKind(kind),
            WindowsKeyCode = windowsKeyCode,
            NativeKeyCode = nativeKeyCode,
            Character = character,
            UnmodifiedCharacter = character,
            Modifiers = Flags(modifiers),
            IsSystemKey = isSystemKey,
        });
    }

    public void SetFocus(bool focused) => _browserHost?.SetFocus(focused);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        FrameReady = null;
        _browserHost?.CloseBrowser(forceClose: true);
    }

    // --- helpers ---------------------------------------------------------------

    private void RunOrDefer(Action<CefBrowser> action)
    {
        if (_disposed) return;
        if (_browser != null)
            action(_browser);
        else
            _deferred.Add(() => action(_browser!));
    }

    private static CefMouseEvent MouseEvent(float x, float y, WebInputModifiers modifiers) =>
        new((int)MathF.Round(x), (int)MathF.Round(y), Flags(modifiers));

    private static CefEventFlags Flags(WebInputModifiers m)
    {
        var f = CefEventFlags.None;
        if (m.HasFlag(WebInputModifiers.Shift)) f |= CefEventFlags.ShiftDown;
        if (m.HasFlag(WebInputModifiers.Control)) f |= CefEventFlags.ControlDown;
        if (m.HasFlag(WebInputModifiers.Alt)) f |= CefEventFlags.AltDown;
        if (m.HasFlag(WebInputModifiers.Meta)) f |= CefEventFlags.CommandDown;
        if (m.HasFlag(WebInputModifiers.LeftMouseButton)) f |= CefEventFlags.LeftMouseButton;
        if (m.HasFlag(WebInputModifiers.MiddleMouseButton)) f |= CefEventFlags.MiddleMouseButton;
        if (m.HasFlag(WebInputModifiers.RightMouseButton)) f |= CefEventFlags.RightMouseButton;
        return f;
    }

    private static CefMouseButtonType MapButton(WebMouseButton b) => b switch
    {
        WebMouseButton.Middle => CefMouseButtonType.Middle,
        WebMouseButton.Right => CefMouseButtonType.Right,
        _ => CefMouseButtonType.Left,
    };

    private static CefKeyEventType MapKeyKind(WebKeyEventKind k) => k switch
    {
        WebKeyEventKind.RawKeyDown => CefKeyEventType.RawKeyDown,
        WebKeyEventKind.KeyUp => CefKeyEventType.KeyUp,
        WebKeyEventKind.Char => CefKeyEventType.Char,
        _ => CefKeyEventType.KeyDown,
    };

    private static WebCursor MapCursor(CefCursorType type) => type switch
    {
        CefCursorType.Hand => WebCursor.Hand,
        CefCursorType.Pointer => WebCursor.Pointer,
        CefCursorType.IBeam => WebCursor.Text,
        CefCursorType.Cross => WebCursor.Crosshair,
        CefCursorType.EastWestResize or CefCursorType.ColumnResize => WebCursor.ResizeHorizontal,
        CefCursorType.NorthSouthResize or CefCursorType.RowResize => WebCursor.ResizeVertical,
        CefCursorType.Wait => WebCursor.Wait,
        CefCursorType.Progress => WebCursor.Progress,
        CefCursorType.NotAllowed or CefCursorType.NoDrop => WebCursor.NotAllowed,
        CefCursorType.None => WebCursor.None,
        _ => WebCursor.Default,
    };
}
