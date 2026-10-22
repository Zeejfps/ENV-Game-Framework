namespace ZGF.Gui.Immediate;

/// <summary>
/// An immediate-mode application, run by <see cref="ImmediateGuiAppHost.Run{T}"/>. <see cref="OnRender"/> describes
/// the entire UI from <typeparamref name="T"/> every frame; widgets report interactions as return
/// values and the app mutates <typeparamref name="T"/> in response. There is no retained tree.
/// </summary>
public abstract class ImmediateGuiApp<T> where T : class
{
    protected ImmediateGuiApp(T state)
    {
        State = state;
    }

    public T State { get; }

    public void Startup(ImmediateGuiAppHost host)
    {
        OnStartup(host);
    }

    public void Render(Ui ui)
    {
        OnRender(ui, State);
    }

    public void Shutdown(ImmediateGuiAppHost host)
    {
        OnShutdown(host);
    }

    /// <summary>Runs on the UI thread before the first frame, which waits for it to return. What it
    /// awaits continues on the UI thread at the start of a later frame.</summary>
    protected virtual void OnStartup(ImmediateGuiAppHost host) { }

    protected abstract void OnRender(Ui ui, T state);

    /// <summary>Runs on the UI thread once the window has closed and no more frames are drawn;
    /// <see cref="ImmediateGuiAppHost.WindowBounds"/> still tells where it was left.</summary>
    protected virtual void OnShutdown(ImmediateGuiAppHost host) { }
}
