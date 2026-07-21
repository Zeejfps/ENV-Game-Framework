using Xilium.CefGlue;

namespace ZGF.WebView.Cef;

/// <summary>
/// The <see cref="CefClient"/> for one off-screen browser: wires the render handler (paint → texture),
/// lifespan (async creation, popup policy), display (title / url / cursor) and the browser side of the
/// JS bridge. Every callback runs on the UI thread thanks to the external message pump, so it forwards
/// straight into the owning <see cref="CefWebView"/>.
/// </summary>
internal sealed class CefWebViewClient : CefClient
{
    private readonly CefRenderHandler _renderHandler;
    private readonly CefLifeSpanHandler _lifeSpanHandler;
    private readonly CefDisplayHandler _displayHandler;
    private readonly CefWebView _owner;

    public CefWebViewClient(CefWebView owner)
    {
        _owner = owner;
        _renderHandler = new RenderHandler(owner);
        _lifeSpanHandler = new LifeSpanHandler(owner);
        _displayHandler = new DisplayHandler(owner);
    }

    protected override CefRenderHandler GetRenderHandler() => _renderHandler;
    protected override CefLifeSpanHandler GetLifeSpanHandler() => _lifeSpanHandler;
    protected override CefDisplayHandler GetDisplayHandler() => _displayHandler;

    // Page → C#: the render process forwards window.zgf.postMessage as a process message.
    protected override bool OnProcessMessageReceived(
        CefBrowser browser, CefFrame frame, CefProcessId sourceProcess, CefProcessMessage message)
    {
        if (message.Name == ZgfCefRenderApp.QueryMessage && message.Arguments.Count >= 1)
        {
            _owner.RaiseMessage(message.Arguments.GetString(0));
            return true;
        }
        return false;
    }

    private sealed class RenderHandler(CefWebView owner) : CefRenderHandler
    {
        protected override CefAccessibilityHandler? GetAccessibilityHandler() => null;

        protected override void GetViewRect(CefBrowser browser, out CefRectangle rect) =>
            owner.GetViewRect(out rect);

        protected override bool GetScreenInfo(CefBrowser browser, CefScreenInfo screenInfo) =>
            owner.GetScreenInfo(screenInfo);

        protected override void OnPaint(
            CefBrowser browser, CefPaintElementType type, CefRectangle[] dirtyRects,
            IntPtr buffer, int width, int height) =>
            owner.OnPaint(type, buffer, width, height);

        protected override void OnAcceleratedPaint(
            CefBrowser browser, CefPaintElementType type, CefRectangle[] dirtyRects, IntPtr sharedHandle)
        {
            // GPU shared-texture fast path (plan perf phase) — not used with software OnPaint.
        }

        protected override void OnPopupSize(CefBrowser browser, CefRectangle rect) { }

        protected override void OnScrollOffsetChanged(CefBrowser browser, double x, double y) { }

        protected override void OnImeCompositionRangeChanged(
            CefBrowser browser, CefRange selectedRange, CefRectangle[] characterBounds)
        {
        }
    }

    private sealed class LifeSpanHandler(CefWebView owner) : CefLifeSpanHandler
    {
        protected override void OnAfterCreated(CefBrowser browser) => owner.OnAfterCreated(browser);

        protected override void OnBeforeClose(CefBrowser browser) => owner.OnBrowserClosed();

        // window.open / target=_blank: cancel the native popup; the app decides what to do (plan
        // decision 9). No new browser windows are spawned by default.
        protected override bool OnBeforePopup(
            CefBrowser browser, CefFrame frame, string targetUrl, string targetFrameName,
            CefWindowOpenDisposition targetDisposition, bool userGesture, CefPopupFeatures popupFeatures,
            CefWindowInfo windowInfo, ref CefClient client, CefBrowserSettings settings,
            ref CefDictionaryValue extraInfo, ref bool noJavascriptAccess) => true;
    }

    private sealed class DisplayHandler(CefWebView owner) : CefDisplayHandler
    {
        protected override void OnTitleChange(CefBrowser browser, string title) =>
            owner.RaiseTitle(title ?? string.Empty);

        protected override void OnAddressChange(CefBrowser browser, CefFrame frame, string url)
        {
            if (frame.IsMain)
                owner.RaiseUrl(url ?? string.Empty);
        }

        protected override bool OnCursorChange(
            CefBrowser browser, IntPtr cursorHandle, CefCursorType type, CefCursorInfo customCursorInfo)
        {
            owner.RaiseCursor(type);
            return true; // handled — we drive the host cursor ourselves.
        }
    }
}
