using ZGF.Desktop;

namespace ZGF.Gui.Immediate;

/// <summary>Where an <see cref="ImmediateGuiApp{TUiState}"/> runs: the window it opens and how that window
/// draws. Nothing opens until <see cref="Run{TUiState}"/>, so the app can exist before its window does.</summary>
public sealed class ImmediateGuiHost
{
    private readonly StartupConfig _config;
    private readonly RenderBackendKind _backend;

    public ImmediateGuiHost(StartupConfig config, RenderBackendKind backend = RenderBackendKind.Auto)
    {
        _config = config;
        _backend = backend;
    }

    /// <summary>Opens the window and runs <paramref name="app"/> in it until the window closes.</summary>
    public void Run<TUiState>(ImmediateGuiApp<TUiState> app)
        where TUiState : class, IUiState
    {
        var host = new ImmediateGuiAppHost<TUiState>(_config, _backend, app.UiState, app.Update);
        try
        {
            using var context = host.InstallSynchronizationContext();
            host.CloseAsked = () => app.CloseRequested(host);
            app.Startup(host);
            host.RunUntilClosed();
            app.Shutdown(host);
        }
        finally
        {
            host.Close();
        }
    }
}
