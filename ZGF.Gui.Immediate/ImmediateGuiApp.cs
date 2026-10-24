using ZGF.Desktop;

namespace ZGF.Gui.Immediate;

/// <summary>
/// An immediate-mode application, started with <see cref="Run"/>.
/// <see cref="OnRender"/> describes the entire UI from <typeparamref name="TAppState"/> every frame;
/// widgets report interactions as return values and the app mutates <typeparamref name="TAppState"/>
/// in response. There is no retained tree. <typeparamref name="TUiState"/> travels on the
/// <see cref="Ui{T}"/>: what views read besides the app's state, such as the theme.
/// </summary>
public abstract class ImmediateGuiApp<TAppState, TUiState>
    where TAppState : class
    where TUiState : class, IUiState
{
    protected ImmediateGuiApp(TAppState state, TUiState uiState)
    {
        State = state;
        UiState = uiState;
    }

    public TAppState State { get; }

    public TUiState UiState { get; }

    /// <summary>Opens the window and runs the app in it until the window closes.</summary>
    public void Run(StartupConfig config, RenderBackendKind backend = RenderBackendKind.Auto)
    {
        var host = new ImmediateGuiAppHost<TUiState>(config, backend, UiState, Render);
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

    internal void Render(Ui<TUiState> ui)
    {
        OnRender(ui, State);
    }

    /// <summary>Runs on the UI thread before the first frame, which waits for it to return. What it
    /// awaits continues on the UI thread at the start of a later frame.</summary>
    protected virtual void OnStartup(ImmediateGuiAppHost<TUiState> host) { }

    protected abstract void OnRender(Ui<TUiState> ui, TAppState state);

    /// <summary>Runs on the UI thread once the window has closed and no more frames are drawn;
    /// <see cref="ImmediateGuiAppHost{TUiState}.WindowBounds"/> still tells where it was left.</summary>
    protected virtual void OnShutdown(ImmediateGuiAppHost<TUiState> host) { }
}
