using ZGF.Gui.Desktop.Controllers;
using ZGF.Gui.Views;
using ZGF.Gui.Widgets;

namespace ZGF.Gui.Desktop.Components.WebView;

/// <summary>
/// A fully-interactive native web view, rendered off-screen into a texture and composited through the
/// ordinary canvas image path — so clipping (rounded corners), opacity, scale, z-order and nesting in a
/// scroll pane all work on it for free. Backed by CEF when the app opts in with <c>UseCefWebView()</c>;
/// with no engine registered it draws a placeholder instead of crashing.
/// <para>App code writes <c>new WebView { Url = "https://example.com" }</c> with no reference to CEF —
/// the widget resolves its <see cref="IWebViewFactory"/> from the build context, mirroring how text
/// inputs resolve <see cref="IClipboard"/>.</para>
/// </summary>
public sealed record WebView : Widget
{
    /// <summary>The URL to load once the browser is ready. Supports the app's custom <c>app://</c>
    /// scheme as well as remote URLs.</summary>
    public string? Url { get; init; }

    /// <summary>Inline HTML to load instead of navigating to <see cref="Url"/>. Wins over
    /// <see cref="Url"/> when both are set.</summary>
    public string? Html { get; init; }

    /// <summary>Raised (on the UI thread) when the page posts a message via
    /// <c>window.zgf.postMessage(string)</c>. Lets the app wire a JS→C# bridge without touching CEF.</summary>
    public Action<string>? OnMessage { get; init; }

    /// <summary>An imperative handle the app can hold to drive the view (navigate, back/forward,
    /// reload, run script) and observe it (title/url/messages) from native code — the browser-chrome
    /// seam. Engine-agnostic: it talks to <see cref="IWebView"/>, never CEF.</summary>
    public WebViewController? Controller { get; init; }

    protected override View CreateView(Context ctx)
    {
        var factory = ctx.Get<IWebViewFactory>();   // null-tolerant, like IClipboard
        var webView = factory?.Create();

        if (webView != null)
        {
            if (OnMessage != null)
                webView.MessageReceived += OnMessage;
            Controller?.Attach(webView);
            if (!string.IsNullOrEmpty(Html))
                webView.LoadHtml(Html);
            else if (!string.IsNullOrEmpty(Url))
                webView.Navigate(Url);
        }

        var view = new WebViewView(ctx.Canvas, webView);

        // Input, focus and cursor need the desktop input system, so — like TextInput — the
        // interactive widget lives here in ZGF.Gui.Desktop rather than in core. The controller forwards
        // ZGF input into the browser and reflects the page's requested cursor.
        if (webView != null)
        {
            var input = ctx.Require<Input.InputSystem>();
            view.UseController(input, () => new WebViewInputController(view, webView, input));
        }

        return view;
    }
}
