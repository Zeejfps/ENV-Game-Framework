using ZGF.Desktop;

namespace ZGF.Gui.Immediate;

/// <summary>
/// An immediate-mode application, run by <see cref="GuiHost.Run{T}"/>. <see cref="OnRender"/> describes
/// the entire UI from <typeparamref name="T"/> every frame; widgets report interactions as return
/// values and the app mutates <typeparamref name="T"/> in response. There is no retained tree.
/// </summary>
public abstract class Gui<T> where T : class
{
    private readonly List<FontFace> _fonts = new();

    protected Gui(T state)
    {
        State = state;
    }

    public T State { get; }

    internal IReadOnlyList<FontFace> Fonts => _fonts;

    public void Startup(GuiHost host)
    {
        OnStartup(host);
    }

    public void Render(Ui ui)
    {
        OnRender(ui, State);
    }

    public void Shutdown(WindowBounds bounds)
    {
        OnShutdown(bounds);
    }

    /// <summary>Runs on the UI thread before the first frame.</summary>
    protected virtual void OnStartup(GuiHost host) { }

    protected abstract void OnRender(Ui ui, T state);

    /// <summary>Runs once the window has closed, with the <paramref name="bounds"/> it was left at —
    /// save them and reopen with <see cref="WindowBounds.ApplyTo"/>.</summary>
    protected virtual void OnShutdown(WindowBounds bounds) { }

    /// <summary>Makes the font in <paramref name="data"/> (TTF/OTF bytes) drawable as
    /// <paramref name="family"/>, for <c>ui.Text(family: ...)</c> and <see cref="UiTheme.FontFamily"/>.
    /// With <paramref name="bold"/> it is the face the family's bold text is drawn in, which is
    /// otherwise synthesized from the regular face; the default font's bold is Inter SemiBold.
    /// Register italic faces under names of their own. Call it from the constructor.</summary>
    protected void RegisterFont(string family, byte[] data, bool bold = false)
    {
        var font = new FontFace(family, data, bold);
        _fonts.Add(font);
    }
}
