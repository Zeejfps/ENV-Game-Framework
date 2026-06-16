namespace ZGF.Desktop;

public interface IWindowedApp : IDisposable
{
    IWindow MainWindow { get; }
    IReadOnlyList<IWindow> Windows { get; }
    IReadOnlyList<MonitorWorkArea> Monitors { get; }
    event Action OnTick;
    void Run();

    /// <summary>Wakes the run loop if it is blocked waiting for OS events. Safe to call from any thread.</summary>
    void Wake();

    /// <summary>Asks the run loop to exit; <see cref="Run"/> returns after the current iteration.</summary>
    void Quit();
    IWindow CreatePopupWindow(in PopupWindowOptions options);
    IWindow CreateWindow(in WindowOptions options);
    void MakeMainContextCurrent();
}
