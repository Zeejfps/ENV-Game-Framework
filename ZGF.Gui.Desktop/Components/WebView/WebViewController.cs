namespace ZGF.Gui.Desktop.Components.WebView;

/// <summary>
/// An imperative handle to a <see cref="WebView"/> for native "browser chrome": create one, pass it as
/// <see cref="WebView.Controller"/>, then drive the page (navigate, back/forward, reload, run script,
/// post a bridge message) and observe it (<see cref="TitleChanged"/>, <see cref="UrlChanged"/>,
/// <see cref="MessageReceived"/>) from ordinary widgets — a toolbar, an address bar, a status label.
/// Engine-agnostic: it forwards to <see cref="IWebView"/> and never references CEF.
/// <para>Calls made before the widget is built are ignored; state and events flow once the view mounts
/// and the controller is attached.</para>
/// </summary>
public sealed class WebViewController
{
    private IWebView? _webView;

    /// <summary>The page's current title, updated as it changes.</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>The page's current top-frame URL, updated on navigation.</summary>
    public string Url { get; private set; } = string.Empty;

    public event Action<string>? TitleChanged;
    public event Action<string>? UrlChanged;
    public event Action<string>? MessageReceived;

    /// <summary>True once the underlying view exists (after the widget has been built).</summary>
    public bool IsAttached => _webView != null;

    internal void Attach(IWebView webView)
    {
        _webView = webView;
        webView.TitleChanged += t => { Title = t; TitleChanged?.Invoke(t); };
        webView.UrlChanged += u => { Url = u; UrlChanged?.Invoke(u); };
        webView.MessageReceived += m => MessageReceived?.Invoke(m);
    }

    public void Navigate(string url) => _webView?.Navigate(url);
    public void LoadHtml(string html, string baseUrl = "about:blank") => _webView?.LoadHtml(html, baseUrl);
    public void GoBack() => _webView?.GoBack();
    public void GoForward() => _webView?.GoForward();
    public void Reload() => _webView?.Reload();
    public void ExecuteScript(string js) => _webView?.ExecuteScript(js);
    public void PostMessage(string json) => _webView?.PostMessage(json);
}
