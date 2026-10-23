namespace ZGF.Gui.Immediate;

/// <summary>
/// An immediate-mode application, run by <see cref="ImmediateGuiAppHost.Run{TAppState, TUiState}"/>.
/// <see cref="OnRender"/> describes the entire UI from <typeparamref name="TAppState"/> every frame;
/// widgets report interactions as return values and the app mutates <typeparamref name="TAppState"/>
/// in response. There is no retained tree. <typeparamref name="TUiState"/> travels on the
/// <see cref="Ui{T}"/>: what views read besides the app's state, such as the theme.
/// </summary>
public abstract class ImmediateGuiApp<TAppState, TUiState>
    where TAppState : class
    where TUiState : class, IUiState
{
    private Ui<TUiState>? _ui;

    protected ImmediateGuiApp(TAppState state, TUiState uiState)
    {
        State = state;
        UiState = uiState;
    }

    public TAppState State { get; }

    public TUiState UiState { get; }

    public void Startup(ImmediateGuiAppHost host)
    {
        OnStartup(host);
    }

    internal Ui CreateUi(ICanvas canvas, IClipboard clipboard, IPopupHost popupHost)
    {
        _ui = new Ui<TUiState>(UiState, canvas, clipboard, popupHost);
        return _ui;
    }

    internal void Render()
    {
        OnRender(_ui!, State);
    }

    public void Shutdown(ImmediateGuiAppHost host)
    {
        OnShutdown(host);
    }

    /// <summary>Runs on the UI thread before the first frame, which waits for it to return. What it
    /// awaits continues on the UI thread at the start of a later frame.</summary>
    protected virtual void OnStartup(ImmediateGuiAppHost host) { }

    protected abstract void OnRender(Ui<TUiState> ui, TAppState state);

    /// <summary>Runs on the UI thread once the window has closed and no more frames are drawn;
    /// <see cref="ImmediateGuiAppHost.WindowBounds"/> still tells where it was left.</summary>
    protected virtual void OnShutdown(ImmediateGuiAppHost host) { }
}
