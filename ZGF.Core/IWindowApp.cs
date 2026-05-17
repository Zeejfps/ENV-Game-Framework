namespace ZGF.Core;

public interface IWindowApp : IDisposable
{
    IntPtr WindowHandle { get; }
    int Width { get; }
    int Height { get; }

    event Action OnUpdate;
    event Action<int, int> OnResize;
    event Action<int, int> OnFramebufferResize;

    void Run();
}
