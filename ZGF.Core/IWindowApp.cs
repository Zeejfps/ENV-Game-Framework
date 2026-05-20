namespace ZGF.Core;

public interface IWindowApp : IDisposable
{
    IntPtr WindowHandle { get; }
    int Width { get; }
    int Height { get; }
    // framebuffer-pixels / window-points; 1.0 on non-HiDPI, typically 2.0 on Retina.
    float DpiScale { get; }

    event Action OnUpdate;
    event Action<int, int> OnResize;
    event Action<int, int> OnFramebufferResize;

    void Run();
}
