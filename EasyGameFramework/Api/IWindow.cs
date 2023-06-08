using EasyGameFramework.Api.Rendering;

namespace EasyGameFramework.Api;

public interface IWindow
{
    event Action Closed;
    
    int ViewportWidth { get; set; }
    int ViewportHeight { get; set; }
    string Title { get; set; }
    bool IsResizable { get; set; }
    int PosX { get; set; }
    int PosY { get; set; }
    bool IsFullscreen { get; set; }
    bool IsVsyncEnabled { get; set; }
    bool IsOpened { get; }
    CursorMode CursorMode { get; set; }
    IGpu Gpu { get; }
    IInputSystem Input { get; }

    void Open();
    void OpenCentered();
    void Close();
    
    void SetViewportSize(int width, int height);
    void SetTopLeftPosition(int x, int y);
}