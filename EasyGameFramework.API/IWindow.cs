using EasyGameFramework.API.AssetTypes;

namespace EasyGameFramework.API;

public interface IWindow
{
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

    void Open();
    void OpenCentered();
    void Close();
    void Update();
    void SetSize(int width, int height);
    void Reposition(int x, int y);
}