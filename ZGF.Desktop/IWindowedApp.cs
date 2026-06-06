namespace ZGF.Desktop;

public interface IWindowedApp : IDisposable
{
    IWindow MainWindow { get; }
    IReadOnlyList<IWindow> Windows { get; }
    IReadOnlyList<MonitorWorkArea> Monitors { get; }
    event Action OnTick;
    void Run();
    IWindow CreatePopupWindow(in PopupWindowOptions options);
    IWindow CreateWindow(in WindowOptions options);
    void MakeMainContextCurrent();
}
