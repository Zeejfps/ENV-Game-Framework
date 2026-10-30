using System.Diagnostics;
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
    private readonly IWebViewFrameSink _frames;
    private readonly CefWebViewOptions _options;
    private readonly WebViewSetup _setup;
    private readonly CefWebViewClient _client;
    private readonly List<Action> _deferred = new();

    private CefBrowser? _browser;
    private CefBrowserHost? _browserHost;
    private string _initialUrl = "about:blank";
    private bool _created;
    private bool _disposed;

    private byte[] _pixels = [];
    private PaintPath _paintPath = new PaintPath.Software();
    private PaintStats _stats;
    private bool _reportedImportFailure;

    // Logical (DIP) size + device scale CEF renders at; OnPaint returns logical*scale device pixels.
    private int _logicalWidth = 1;
    private int _logicalHeight = 1;
    private float _dpi = 1f;

    public CefWebView(CefRuntimeHost host, IWebViewFrameSink frames, CefWebViewOptions options, WebViewSetup setup)
    {
        _setup = setup;
        _host = host;
        _frames = frames;
        _options = options;
        _client = new CefWebViewClient(this);
        _stats = new PaintStats(Stopwatch.GetTimestamp());
    }

    public event Action<string>? MessageReceived;
    public event Action<string>? TitleChanged;
    public event Action<string>? UrlChanged;
    public event Action<WebCursor>? CursorChanged;

    // --- Creation (driven by CefRuntimeHost once CEF is initialised) -----------

    internal void CreateBrowser()
    {
        if (_created || _disposed)
            return;
        _created = true;

        _paintPath = ChoosePaintPath();
        var windowInfo = CefWindowInfo.Create();
        windowInfo.SetAsWindowless(IntPtr.Zero, transparent: false);
        windowInfo.WindowlessRenderingEnabled = true;
        windowInfo.SharedTextureEnabled = _paintPath is PaintPath.Accelerated;

        var settings = new CefBrowserSettings
        {
            WindowlessFrameRate = _options.FrameRate,
            // Opaque background sidesteps CEF's premultiplied-alpha OnPaint buffer (plan decision 5).
            BackgroundColor = new CefColor(255, 255, 255, 255),
        };

        CefLog.Write($"CreateBrowser url={_initialUrl} logical={_logicalWidth}x{_logicalHeight} paint={_paintPath}");
        CefBrowserHost.CreateBrowser(windowInfo, _client, settings, _initialUrl, ExtraInfo(), null);
    }

    private CefDictionaryValue? ExtraInfo()
    {
        var scripts = _setup.DocumentStartScripts;
        if (scripts.Count == 0)
            return null;
        var list = CefListValue.Create();
        list.SetSize(scripts.Count);
        for (var i = 0; i < scripts.Count; i++)
            list.SetString(i, scripts[i]);
        var extraInfo = CefDictionaryValue.Create();
        extraInfo.SetList(ZgfCefRenderApp.DocumentStartScriptsKey, list);
        return extraInfo;
    }

    // --- Handler callbacks (all on the UI thread) -----------------------------

    internal void OnAfterCreated(CefBrowser browser)
    {
        CefLog.Write("OnAfterCreated");
        _browser = browser;
        _browserHost = browser.GetHost();
        _host.BrowserOpened();
        if (_disposed)
        {
            _browserHost.CloseBrowser(forceClose: true);
            return;
        }
        _browserHost.WasResized();
        foreach (var action in _deferred)
            action();
        _deferred.Clear();
    }

    internal void OnBrowserClosed()
    {
        _browser = null;
        _browserHost = null;
        _host.BrowserClosed();
    }

    // Rate-limited auto-reload to recover from a render-process crash.
    //
    // On some systems a heavy off-screen page fires a renderer termination ~once during load — a
    // Chromium __fastfail (STATUS_STACK_BUFFER_OVERRUN / 0xC0000409) that Chromium itself recovers from
    // by relaunching the render process, after which the page still paints. Reloading on *every*
    // termination fought that built-in recovery and amplified a single crash into a reload→crash→reload
    // storm (measured ~2 crashes/load vs ~1 with no reload, no better paint). So recovery is now
    // rate-limited: reload at most once per interval, and let Chromium's own recovery absorb the rapid
    // re-crashes. A genuinely frozen surface — a "web view stopped responding" where crashes are spaced
    // out — still gets bounded reloads. Each user Navigate() resets the budget (see Navigate).
    private int _crashReloads;
    private DateTime _lastReloadUtc = DateTime.MinValue;
    private const int MaxCrashReloads = 1;
    private const double MinReloadIntervalSeconds = 4;

    internal void OnRenderProcessGone(CefTerminationStatus status)
    {
        var url = _browser?.GetMainFrame()?.Url ?? _initialUrl;
        if (_disposed || _browserHost == null)
        {
            CefLog.Error($"render process {Describe(status)} on {url}; view is disposed, not recovering");
            return;
        }
        var now = DateTime.UtcNow;
        if ((now - _lastReloadUtc).TotalSeconds < MinReloadIntervalSeconds)
        {
            // A re-crash right after a reload — don't storm; Chromium is already relaunching the renderer.
            CefLog.Error($"render process {Describe(status)} on {url}; within {MinReloadIntervalSeconds}s of a reload — deferring to Chromium's own recovery");
            return;
        }
        if (_crashReloads++ >= MaxCrashReloads)
        {
            CefLog.Error($"render process {Describe(status)} on {url}; " +
                         $"reload budget ({MaxCrashReloads}) exhausted — leaving the page blank.");
            return;
        }
        _lastReloadUtc = now;
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

    private PaintPath ChoosePaintPath()
    {
        switch (_options.PaintPath)
        {
            case CefPaintPath.Software:
                return new PaintPath.Software();
            case CefPaintPath.PreferAccelerated:
                var kind = PlatformSharedTextureKind();
                return kind != null && _frames.AcceptsSharedTextures(kind.Value)
                    ? new PaintPath.Accelerated(kind.Value)
                    : new PaintPath.Software();
            default:
                throw new ArgumentOutOfRangeException(nameof(_options.PaintPath), _options.PaintPath, null);
        }
    }

    /// <summary>What CEF's <c>OnAcceleratedPaint</c> hands over on this platform, if it is one we import.</summary>
    private static SharedTextureKind? PlatformSharedTextureKind()
    {
        if (OperatingSystem.IsWindows())
            return SharedTextureKind.D3D11NtHandle;
        if (OperatingSystem.IsMacOS())
            return SharedTextureKind.IOSurface;
        return null;
    }

    internal unsafe void OnPaint(CefPaintElementType type, IntPtr buffer, int width, int height)
    {
        if (_disposed || type != CefPaintElementType.View || width <= 0 || height <= 0)
            return; // Popup element (e.g. <select> dropdown) compositing is deferred (plan decision 9).

        var started = Stopwatch.GetTimestamp();
        var needed = width * height * 4;
        if (_pixels.Length < needed)
            _pixels = new byte[needed];

        // CEF hands a top-left-origin BGRA buffer; swizzle to RGBA.
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

        _frames.OnPixels(width, height, _pixels.AsSpan(0, needed));
        RecordPaint(started, "software", width, height);
    }

    internal void OnAcceleratedPaint(CefPaintElementType type, in CefAcceleratedPaintInfo info)
    {
        if (_disposed || type != CefPaintElementType.View || _paintPath is not PaintPath.Accelerated accelerated)
            return;

        var started = Stopwatch.GetTimestamp();
        if (_stats.Total == 0)
            CefLog.Write($"OnAcceleratedPaint format={info.Format} coded={info.CodedSize.Width}x{info.CodedSize.Height} visible={info.VisibleRect.Width}x{info.VisibleRect.Height}");
        var imported = _frames.OnSharedTexture(new SharedTexture(accelerated.Kind, info.SharedTextureHandle));
        if (!imported && !_reportedImportFailure)
        {
            _reportedImportFailure = true;
            CefLog.Error("the canvas could not import a shared texture; the page will not be shown");
        }
        RecordPaint(started, "accelerated", info.VisibleRect.Width, info.VisibleRect.Height);
    }

    private void RecordPaint(long started, string path, int width, int height)
    {
        var now = Stopwatch.GetTimestamp();
        _stats = _stats.Add(now - started);
        if (!_options.ReportPaintStats)
            return;
        var elapsed = Stopwatch.GetElapsedTime(_stats.WindowStart, now);
        if (elapsed.TotalSeconds < 1)
            return;
        Console.Error.WriteLine(
            $"[zgf-cef] paints {_stats.InWindow / elapsed.TotalSeconds:F1}/s {path} {width}x{height}, " +
            $"callback avg {_stats.AverageMs:F3} ms max {_stats.MaxMs:F3} ms");
        _stats = _stats.StartWindow(now);
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

    // --- IWebView: navigation / scripting --------------------------------------

    public void Navigate(string url)
    {
        if (_disposed) return;
        _crashReloads = 0; // a fresh navigation is a fresh page — restore its crash-recovery budget.
        _lastReloadUtc = DateTime.MinValue;
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

    private abstract record PaintPath
    {
        public sealed record Software : PaintPath;
        public sealed record Accelerated(SharedTextureKind Kind) : PaintPath;
    }

    private readonly record struct PaintStats(long WindowStart, long InWindow = 0, long Total = 0, long TicksInWindow = 0, long MaxTicks = 0)
    {
        public double AverageMs => InWindow == 0 ? 0 : TicksInWindow * 1000.0 / Stopwatch.Frequency / InWindow;
        public double MaxMs => MaxTicks * 1000.0 / Stopwatch.Frequency;

        public PaintStats Add(long ticks) =>
            this with { InWindow = InWindow + 1, Total = Total + 1, TicksInWindow = TicksInWindow + ticks, MaxTicks = Math.Max(MaxTicks, ticks) };

        public PaintStats StartWindow(long now) => this with { WindowStart = now, InWindow = 0, TicksInWindow = 0, MaxTicks = 0 };
    }
}
