using ZGF.Gui;

namespace ZGF.WebView.Cef;

/// <summary>Creates CEF-backed <see cref="IWebView"/>s and registers each with the shared runtime host
/// so its browser is created as soon as CEF is ready.</summary>
internal sealed class CefWebViewFactory : IWebViewFactory
{
    private readonly CefRuntimeHost _host;

    public CefWebViewFactory(CefRuntimeHost host)
    {
        _host = host;
    }

    public IWebView Create()
    {
        var webView = new CefWebView(_host);
        _host.RegisterWebView(webView);
        return webView;
    }
}
