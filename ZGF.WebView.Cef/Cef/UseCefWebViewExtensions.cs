using ZGF.Gui;
using ZGF.Gui.Desktop;
using ZGF.Observable;

namespace ZGF.WebView.Cef;

/// <summary>
/// The single CEF-referencing symbol an app touches. <c>UseCefWebView()</c> registers the web-view
/// factory and a hosted service that owns the whole CEF lifecycle (init on start, pump per schedule,
/// shutdown on dispose). The app writes nothing in <c>Main</c> and nothing in its csproj beyond a
/// project reference to <c>ZGF.WebView.Cef</c>.
/// </summary>
public static class UseCefWebViewExtensions
{
    public static GuiAppBuilder UseCefWebView(this GuiAppBuilder builder) =>
        builder.UseCefWebView(new CefWebViewOptions());

    public static GuiAppBuilder UseCefWebView(this GuiAppBuilder builder, CefWebViewOptions options)
    {
        var context = builder.Context;
        context.AddSingleton(ctx => new CefRuntimeHost(ctx.Require<IUiDispatcher>(), options));
        context.AddSingleton<IWebViewFactory>(ctx => new CefWebViewFactory(ctx.Require<CefRuntimeHost>(), options));
        context.AddHostedService(ctx => new CefHostedService(ctx.Require<CefRuntimeHost>()));
        return builder;
    }
}
