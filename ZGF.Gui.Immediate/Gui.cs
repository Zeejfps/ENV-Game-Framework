using ZGF.Desktop;

namespace ZGF.Gui.Immediate;

/// <summary>
/// An immediate-mode application. <see cref="Render"/> describes the entire UI from
/// <typeparamref name="T"/> every frame; widgets report interactions as return values and the
/// app mutates <typeparamref name="T"/> in response. There is no retained tree.
/// </summary>
public abstract class Gui<T> where T : class
{
    private readonly List<(string Family, byte[] Data)> _fonts = new();
    private ImmediateHost? _host;

    protected Gui(T state)
    {
        State = state;
    }

    public T State { get; }

    /// <summary>Gives popup windows their native look and behavior (no taskbar entry, no
    /// activation, click-through tooltips, dismissal on title-bar presses). Set before
    /// <see cref="Run"/>; without one, popups are plain borderless windows.</summary>
    public IPopupNativeDecorator? PopupDecorator { get; set; }

    protected abstract void Render(Ui ui, T state);

    /// <summary>Makes the font in <paramref name="data"/> (TTF/OTF bytes) drawable as
    /// <paramref name="family"/>, for <c>ui.Text(family: ...)</c> and <see cref="UiStyle.FontFamily"/>.
    /// One family is one face: register bold or italic faces under names of their own.</summary>
    public void RegisterFont(string family, byte[] data)
    {
        _fonts.Add((family, data));
        _host?.RegisterFont(family, data);
    }

    /// <summary>Schedules a frame after <see cref="State"/> changed outside <see cref="Render"/>.
    /// Safe from any thread.</summary>
    public void Invalidate() => _host?.Invalidate();

    /// <summary>Runs <paramref name="action"/> on the UI thread before the next frame, which it
    /// schedules. Safe from any thread while running — the way background work hands back
    /// results.</summary>
    public void Post(Action action) =>
        (_host ?? throw new InvalidOperationException("The app is not running.")).Post(action);

    public void Run(StartupConfig config, RenderBackendKind backend = RenderBackendKind.Auto)
    {
        using var host = new ImmediateHost(config, backend, _fonts, PopupDecorator, ui => Render(ui, State));
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
