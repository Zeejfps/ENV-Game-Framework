namespace ZGF.Core;

public interface IWindow : IDisposable
{
    IntPtr WindowHandle { get; }
    int Width { get; }
    int Height { get; }
    float DpiScale { get; }
    bool IsVisible { get; }
    bool NeedsRedraw { get; set; }

    event Action<int, int> OnResize;
    event Action<int, int> OnFramebufferResize;
    event Action<bool> OnFocusChanged;
    event Action OnClose;

    void Show();
    void Hide();
    void SetPosition(int screenX, int screenY);
    void SetSize(int widthPoints, int heightPoints);
    void RequestRedraw();
    void RenderNow();
    void MakeContextCurrent();
}
