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
    private readonly CefLoadHandler _loadHandler;
    private readonly CefRequestHandler _requestHandler;
    private readonly CefWebView _owner;

    public CefWebViewClient(CefWebView owner)
    {
        _owner = owner;
        _renderHandler = new RenderHandler(owner);
        _lifeSpanHandler = new LifeSpanHandler(owner);
        _displayHandler = new DisplayHandler(owner);
        _loadHandler = new LoadHandler();
        _requestHandler = new RequestHandler(owner);
    }

    protected override CefRenderHandler GetRenderHandler() => _renderHandler;
    protected override CefLifeSpanHandler GetLifeSpanHandler() => _lifeSpanHandler;
    protected override CefDisplayHandler GetDisplayHandler() => _displayHandler;
    protected override CefLoadHandler GetLoadHandler() => _loadHandler;
    protected override CefRequestHandler GetRequestHandler() => _requestHandler;

    // Surfaces the fault that most often explains a blank/frozen web view: the render process died.
    // CEF keeps the browser host alive across a renderer crash, so we log the cause and reload once to
    // recover (bounded per navigation in CefWebView) instead of leaving a dead surface.
    private sealed class RequestHandler(CefWebView owner) : CefRequestHandler
    {
        protected override CefResourceRequestHandler? GetResourceRequestHandler(
            CefBrowser browser, CefFrame frame, CefRequest request, bool isNavigation,
            bool isDownload, string requestInitiator, ref bool disableDefaultHandling) => null;

        // CEF 150: the callback gained errorCode + errorString (they name the fatal reason directly).
        protected override void OnRenderProcessTerminated(
            CefBrowser browser, CefTerminationStatus status, int errorCode, string errorString)
        {
            if (errorCode != 0 || !string.IsNullOrEmpty(errorString))
                CefLog.Error($"renderer termination detail: code={errorCode} '{errorString}'");
            owner.OnRenderProcessGone(status);
        }
    }

    private sealed class LoadHandler : CefLoadHandler
    {
        protected override void OnLoadError(
            CefBrowser browser, CefFrame frame, CefErrorCode errorCode, string errorText, string failedUrl)
        {
            // ERR_ABORTED is normal — a navigation replaced by another, or a cancelled subresource.
            if (errorCode == CefErrorCode.Aborted)
                return;
            CefLog.Error($"load failed [{errorCode}] {(frame.IsMain ? "main frame" : "subframe")} " +
                         $"{failedUrl}: {errorText}");
        }
    }

    // Page → C#: the render process forwards window.zgf.postMessage as a process message.
    protected override bool OnProcessMessageReceived(
        CefBrowser browser, CefFrame frame, CefProcessId sourceProcess, CefProcessMessage message)
    {
        if (message.Name == ZgfCefRenderApp.QueryMessage && message.Arguments.Count >= 1)
        {
            if (!frame.IsMain)
                return true;
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
            CefBrowser browser, CefPaintElementType type, CefRectangle[] dirtyRects, CefAcceleratedPaintInfo info) =>
            owner.OnAcceleratedPaint(type, info);

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
        // CEF 150: signature gained a popupId before targetUrl.
        protected override bool OnBeforePopup(
            CefBrowser browser, CefFrame frame, int popupId, string targetUrl, string targetFrameName,
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

        // Page JS diagnostics — a console error is often the last thing logged before a renderer dies,
        // so errors/warnings always surface; info/debug/verbose only when ZGF_CEF_DEBUG is set.
        protected override bool OnConsoleMessage(
            CefBrowser browser, CefLogSeverity level, string message, string source, int line)
        {
            var where = string.IsNullOrEmpty(source) ? "" : $" ({source}:{line})";
            if (level is CefLogSeverity.Error or CefLogSeverity.Fatal or CefLogSeverity.Warning)
                CefLog.Error($"page console [{level}]{where} {message}");
            else
                CefLog.Write($"page console [{level}]{where} {message}");
            return false; // let CEF apply its default handling too.
        }
    }
}
