using System.Collections.Concurrent;
using ZGF.Desktop;

namespace ZGF.Gui.Immediate;

public enum RenderBackendKind
{
    Auto,
    OpenGl,
    Metal,
}

/// <summary>
/// An immediate-mode application. <see cref="Render"/> describes the entire UI from
/// <typeparamref name="T"/> every frame; widgets report interactions as return values and the
/// app mutates <typeparamref name="T"/> in response. There is no retained tree.
/// </summary>
public abstract class Gui<T> where T : class
{
    private readonly List<(string Family, byte[] Data, bool Bold)> _fonts = new();
    private readonly ConcurrentQueue<Action> _posted = new();
    private volatile ImmediateHost? _host;
    private volatile bool _stopped;
    private WindowBounds _bounds;

    protected Gui(T state)
    {
        State = state;
    }

    public T State { get; }

    /// <summary>The main window's bounds while running, and as it was left once <see cref="Run"/>
    /// returns — save them and reopen with <see cref="WindowBounds.ApplyTo"/>.</summary>
    public WindowBounds WindowBounds => _host?.Bounds ?? _bounds;

    protected abstract void Render(Ui ui, T state);

    /// <summary>Runs on the UI thread before the first frame.</summary>
    protected virtual void OnStartup() { }

    /// <summary>Runs once the window has closed and <see cref="Run"/> is about to return.</summary>
    protected virtual void OnShutdown() { }

    /// <summary>Makes the font in <paramref name="data"/> (TTF/OTF bytes) drawable as
    /// <paramref name="family"/>, for <c>ui.Text(family: ...)</c> and <see cref="UiTheme.FontFamily"/>.
    /// With <paramref name="bold"/> it is the face the family's bold text is drawn in, which is
    /// otherwise synthesized from the regular face; the default font's bold is Inter SemiBold.
    /// Register italic faces under names of their own.</summary>
    public void RegisterFont(string family, byte[] data, bool bold = false)
    {
        _fonts.Add((family, data, bold));
        _host?.RegisterFont(family, data, bold);
    }

    /// <summary>Schedules a frame after <see cref="State"/> changed outside <see cref="Render"/>.
    /// Safe from any thread.</summary>
    public void Invalidate() => _host?.Invalidate();

    /// <summary>Runs <paramref name="action"/> on the UI thread before the next frame, which it
    /// schedules — the way background work hands back results. Safe from any thread at any time:
    /// before <see cref="Run"/> it waits for the first frame, after it returns it is dropped. While
    /// <see cref="Run"/> runs, <c>await</c> on the UI thread continues this way too.</summary>
    public void Post(Action action)
    {
        if (_stopped) return;
        _posted.Enqueue(action);
        _host?.Invalidate();
    }

    public void Run(StartupConfig config, RenderBackendKind backend = RenderBackendKind.Auto)
    {
        _stopped = false;
        Post(OnStartup);
        using var context = new UiSynchronizationContext(Post).Install();
        using var host = new ImmediateHost(config, backend, _fonts, _posted, ui => Render(ui, State));
        _host = host;
        try
        {
            host.Run();
        }
        finally
        {
            _bounds = host.Bounds;
            _host = null;
            _stopped = true;
            _posted.Clear();
        }
        OnShutdown();
    }
}
