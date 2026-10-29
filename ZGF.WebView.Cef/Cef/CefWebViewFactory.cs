using ZGF.Gui;

namespace ZGF.WebView.Cef;

/// <summary>Creates CEF-backed <see cref="IWebView"/>s and registers each with the shared runtime host
/// so its browser is created as soon as CEF is ready.</summary>
internal sealed class CefWebViewFactory : IWebViewFactory
{
    private readonly CefRuntimeHost _host;
    private readonly CefWebViewOptions _options;

    public CefWebViewFactory(CefRuntimeHost host, CefWebViewOptions options)
    {
        _host = host;
        _options = options;
    }

    public IWebView Create(IWebViewFrameSink frames)
    {
        var webView = new CefWebView(_host, frames, _options);
        _host.RegisterWebView(webView);
        return webView;
    }
}
