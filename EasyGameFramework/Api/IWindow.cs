using EasyGameFramework.Api.AssetTypes;

namespace EasyGameFramework.Api;

public interface IWindow
{
    event Action Closed;
    
    int Width { get; set; }
    int Height { get; set; }
    string Title { get; set; }
    bool IsResizable { get; set; }
    int PosX { get; set; }
    int PosY { get; set; }
    bool IsFullscreen { get; set; }
    bool IsVsyncEnabled { get; set; }
    bool IsOpened { get; }
    IGpuFramebuffer Framebuffer { get; }
    CursorMode CursorMode { get; set; }
    
    void Open();
    void OpenCentered();
    void Close();
    
    void SetSize(int width, int height);
    void SetPosition(int x, int y);
}