namespace ZGF.Desktop;

public interface IWindowedApp : IDisposable
{
    IWindow MainWindow { get; }
    IReadOnlyList<IWindow> Windows { get; }
    IReadOnlyList<MonitorWorkArea> Monitors { get; }

    // True while any of this app's windows holds OS focus. Not MainWindow.IsFocused: a popup
    // (which is the key window on macOS) or a secondary window taking focus keeps the app in the
    // foreground, and the main window blurs every time a menu opens.
    bool IsForeground { get; }
    event Action<bool> OnForegroundChanged;

    event Action OnTick;
    void Run();

    /// <summary>Wakes the run loop if it is blocked waiting for OS events. Safe to call from any thread.</summary>
    void Wake();

    /// <summary>Asks the run loop to exit; <see cref="Run"/> returns after the current iteration.</summary>
    void Quit();
    IWindow CreatePopupWindow(in PopupWindowOptions options);
    IWindow CreateWindow(in WindowOptions options);
}
