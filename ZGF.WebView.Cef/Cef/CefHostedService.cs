using ZGF.Gui;

namespace ZGF.WebView.Cef;

/// <summary>
/// Drives the CEF runtime lifecycle as a ZGF hosted service. <see cref="Start"/> runs after the app is
/// built and the window exists, which is exactly when <c>CefRuntime.Initialize</c> may run; disposal
/// (when the app's <see cref="Context"/> is disposed) shuts CEF down. The <c>ExecuteProcess</c> gate is
/// gone entirely — it lives in the separate <c>zgf-cef-helper</c> exe — so nothing here belongs in the
/// app's <c>Main</c>.
/// </summary>
internal sealed class CefHostedService : IHostedService, IDisposable
{
    private readonly CefRuntimeHost _host;

    public CefHostedService(CefRuntimeHost host)
    {
        _host = host;
    }

    public void Start() => _host.Initialize();

    public void Dispose() => _host.Dispose();
}
