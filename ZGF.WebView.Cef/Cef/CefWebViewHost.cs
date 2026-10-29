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

    private CefWebViewHost(CefRuntimeHost runtime, CefWebViewOptions options)
    {
        _runtime = runtime;
        _factory = new CefWebViewFactory(runtime, options);
    }

    /// <summary>Starts CEF. Call it on the UI thread, which <paramref name="dispatcher"/> runs CEF's pump
    /// on; every web view callback then arrives there too.</summary>
    public static CefWebViewHost Start(IUiDispatcher dispatcher, CefWebViewOptions? options = null)
    {
        var runtime = new CefRuntimeHost(dispatcher);
        runtime.Initialize();
        return new CefWebViewHost(runtime, options ?? new CefWebViewOptions());
    }

    public IWebView Create(IWebViewFrameSink frames, WebViewSetup setup) => _factory.Create(frames, setup);

    public void Dispose() => _runtime.Dispose();
}
