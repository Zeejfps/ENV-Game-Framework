using System.Numerics;
using EasyGameFramework.Api.Rendering;

namespace EasyGameFramework.Api;

public interface IWindow
{
    event Action Closed;
    
    int ScreenWidth { get; set; }
    int ScreenHeight { get; set; }
    string Title { get; set; }
    bool IsResizable { get; set; }
    int PosX { get; set; }
    int PosY { get; set; }
    bool IsFullscreen { get; set; }
    bool IsVsyncEnabled { get; set; }
    bool IsOpened { get; }
    bool IsFocused { get; }
    CursorMode CursorMode { get; set; }
    IGpu Gpu { get; }
    IInputSystem Input { get; }

    void Open();
    void OpenCentered();
    void Close();
    
    void SetScreenSize(int width, int height);
    void SetTopLeftPosition(int x, int y);
    void PollEvents();
    void SwapBuffers();
    
    Vector2 ScreenToViewportPoint(Vector2 screenPoint, IViewport viewport);
}