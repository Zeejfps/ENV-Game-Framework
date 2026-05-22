namespace ZGF.Core;

public interface IApp : IDisposable
{
    IWindow MainWindow { get; }
    IReadOnlyList<IWindow> Windows { get; }
    event Action OnTick;
    void Run();
    IWindow CreatePopupWindow(in PopupWindowOptions options);
    void MakeMainContextCurrent();
}
