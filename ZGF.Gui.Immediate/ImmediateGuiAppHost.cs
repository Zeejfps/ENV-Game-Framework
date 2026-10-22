using ZGF.Desktop;

namespace ZGF.Gui.Immediate;

public enum RenderBackendKind
{
    Auto,
    OpenGl,
    Metal,
}

/// <summary>The window, render loop and UI thread a <see cref="ImmediateGuiApp{T}"/> runs in. While it runs,
/// <c>await</c> on the UI thread continues before the next frame.</summary>
public sealed class ImmediateGuiAppHost
{
    private readonly ImmediateHost _host;

    private ImmediateGuiAppHost(ImmediateHost host)
    {
        _host = host;
    }

    /// <summary>Opens the window and runs <paramref name="app"/> in it until the window closes.</summary>
    public static void Run<T>(ImmediateGuiApp<T> app, StartupConfig config, RenderBackendKind backend = RenderBackendKind.Auto)
        where T : class
    {
        using var host = new ImmediateHost(config, backend, app.Render);
        using var context = host.InstallSynchronizationContext();
        var guiHost = new ImmediateGuiAppHost(host);
        app.Startup(guiHost);
        host.Run();
        app.Shutdown(guiHost);
    }

    /// <summary>The window's bounds, and once it has closed, where it was left — save them and
    /// reopen with <see cref="WindowBounds.ApplyTo"/>.</summary>
    public WindowBounds WindowBounds => _host.Bounds;

    /// <summary>Schedules a frame after the app's state changed outside a frame. Safe from any thread,
    /// and does nothing once the window has closed.</summary>
    public void Invalidate()
    {
        _host.Invalidate();
    }

    /// <summary>Makes <paramref name="font"/> drawable as its family, for <c>ui.Text(family: ...)</c>
    /// and <see cref="UiTheme.FontFamily"/>. A bold face is the one the family's bold text is drawn
    /// in, which is otherwise synthesized from the regular face; the default font's bold is Inter
    /// SemiBold. Register italic faces under families of their own. Call it on the UI thread, such as
    /// from <see cref="ImmediateGuiApp{T}.OnStartup"/> or after awaiting a background load there; it
    /// does nothing once the window has closed.</summary>
    public void RegisterFont(FontFace font)
    {
        _host.RegisterFont(font);
    }
}
