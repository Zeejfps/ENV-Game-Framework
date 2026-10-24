using ZGF.Desktop;

namespace ZGF.Gui.Immediate;

/// <summary>
/// An immediate-mode application, started with <see cref="Run"/>.
/// <see cref="OnUpdate"/> describes the entire UI from the app's own state every frame;
/// widgets report interactions as return values and the app mutates its state in response.
/// There is no retained tree. <typeparamref name="TUiState"/> travels on the
/// <see cref="Ui{T}"/>: what views read besides the app's state, such as the theme.
/// </summary>
public abstract class ImmediateGuiApp<TUiState>
    where TUiState : class, IUiState
{
    protected ImmediateGuiApp(TUiState uiState)
    {
        UiState = uiState;
    }

    public TUiState UiState { get; }

    /// <summary>Opens the window and runs the app in it until the window closes.</summary>
    public void Run(StartupConfig config, RenderBackendKind backend = RenderBackendKind.Auto)
    {
        var host = new ImmediateGuiAppHost<TUiState>(config, backend, UiState, Update);
        try
        {
            using var context = host.InstallSynchronizationContext();
            OnStartup(host);
            host.RunUntilClosed();
            OnShutdown(host);
        }
        finally
        {
            host.Close();
        }
    }

    internal void Update(Ui<TUiState> ui)
    {
        OnUpdate(ui);
    }

    /// <summary>Runs on the UI thread before the first frame, which waits for it to return. What it
    /// awaits continues on the UI thread at the start of a later frame.</summary>
    protected virtual void OnStartup(ImmediateGuiAppHost<TUiState> host) { }

    protected abstract void OnUpdate(Ui<TUiState> ui);

    /// <summary>Runs on the UI thread once the window has closed and no more frames are drawn;
    /// <see cref="ImmediateGuiAppHost{TUiState}.WindowBounds"/> still tells where it was left.</summary>
    protected virtual void OnShutdown(ImmediateGuiAppHost<TUiState> host) { }
}
