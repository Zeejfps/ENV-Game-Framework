using ZGF.Desktop;

namespace ZGF.Gui.Immediate;

/// <summary>
/// An immediate-mode application. <see cref="Render"/> describes the entire UI from
/// <typeparamref name="T"/> every frame; widgets report interactions as return values and the
/// app mutates <typeparamref name="T"/> in response. There is no retained tree.
/// </summary>
public abstract class Gui<T> where T : class
{
    private ImmediateHost? _host;

    protected Gui(T state)
    {
        State = state;
    }

    public T State { get; }

    protected abstract void Render(Ui ui, T state);

    /// <summary>The window's style, asked before every frame so it can follow the state.</summary>
    protected virtual UiStyle StyleFor(T state) => UiStyle.Dark;

    /// <summary>Schedules a frame after <see cref="State"/> changed outside <see cref="Render"/>.
    /// Safe from any thread.</summary>
    public void Invalidate() => _host?.Invalidate();

    public void Run(StartupConfig config, RenderBackendKind backend = RenderBackendKind.Auto)
    {
        using var host = new ImmediateHost(config, backend, () => StyleFor(State), ui => Render(ui, State));
        _host = host;
        try
        {
            host.Run();
        }
        finally
        {
            _host = null;
        }
    }
}
