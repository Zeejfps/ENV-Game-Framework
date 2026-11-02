using ZGF.Gui;
using ZGF.Observable;

namespace ZGF.WebView.Cef;

/// <summary>
/// CEF for an app that builds no <see cref="Gui.Desktop.GuiAppBuilder"/>, such as an immediate-mode one:
/// <see cref="Start"/> starts the runtime, the host creates web views, and disposing it shuts the runtime
/// down once their browsers have closed. Dispose the web views first.
/// </summary>
public sealed class CefWebViewHost : IWebViewFactory, IDisposable
{
    private readonly CefRuntimeHost _runtime;
    private readonly CefWebViewFactory _factory;
    private readonly CefWebViewOptions _options;

    private CefWebViewHost(CefRuntimeHost runtime, CefWebViewOptions options)
    {
        _runtime = runtime;
        _options = options;
        _factory = new CefWebViewFactory(runtime, options);
    }

    /// <summary>Starts CEF. Call it on the UI thread, which <paramref name="dispatcher"/> runs CEF's pump
    /// on; every web view callback then arrives there too.</summary>
    public static CefWebViewHost Start(IUiDispatcher dispatcher, CefWebViewOptions? options = null)
    {
        var chosen = options ?? new CefWebViewOptions();
        var runtime = new CefRuntimeHost(dispatcher, chosen);
        runtime.Initialize();
        return new CefWebViewHost(runtime, chosen);
    }

    public IWebView Create(IWebViewFrameSink frames, WebViewSetup setup) => _factory.Create(frames, setup);

    /// <summary>
    /// The kind of GPU texture its browsers ask a page's frame sink to take their frames as; null when they paint
    /// into pixels. A canvas asked the first time whether it supports a kind may set up for it then, as the OpenGL
    /// canvas does its Direct3D bridge, holding the UI thread for a fifth of a second or so; asking it before the
    /// first page opens, at a moment nothing moves on screen, keeps that from stalling the page opening.
    /// </summary>
    public SharedTextureKind? SharedTextures =>
        _options.PaintPath == CefPaintPath.PreferAccelerated ? CefWebView.PlatformSharedTextureKind() : null;

    public void Dispose() => _runtime.Dispose();
}
