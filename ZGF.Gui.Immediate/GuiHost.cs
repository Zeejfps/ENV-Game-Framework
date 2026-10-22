using ZGF.Desktop;

namespace ZGF.Gui.Immediate;

public enum RenderBackendKind
{
    Auto,
    OpenGl,
    Metal,
}

/// <summary>The window, render loop and UI thread a <see cref="Gui{T}"/> runs in. While it runs,
/// <c>await</c> on the UI thread continues before the next frame.</summary>
public sealed class GuiHost
{
    private readonly ImmediateHost _host;

    private GuiHost(ImmediateHost host)
    {
        _host = host;
    }

    /// <summary>Opens the window and runs <paramref name="app"/> in it until the window closes.</summary>
    public static void Run<T>(Gui<T> app, StartupConfig config, RenderBackendKind backend = RenderBackendKind.Auto)
        where T : class
    {
        using var host = new ImmediateHost(config, backend, app.Fonts, app.Render);
        using var context = new UiSynchronizationContext(host.Post).Install();
        var guiHost = new GuiHost(host);
        host.Post(() => app.Startup(guiHost));
        host.Run();
        app.Shutdown(host.Bounds);
    }

    /// <summary>Schedules a frame after the app's state changed outside a frame. Safe from any thread,
    /// and does nothing once the window has closed.</summary>
    public void Invalidate()
    {
        _host.Invalidate();
    }
}
