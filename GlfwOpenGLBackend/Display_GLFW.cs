using EasyGameFramework.API;
using Framework;
using GLFW;
using Monitor = GLFW.Monitor;

namespace Framework.GLFW.NET;

class Display_GLFW : IDisplay
{
    public int ResolutionX { get; }
    public int ResolutionY { get; }
    public int RefreshRate { get; }


    public Display_GLFW(Monitor monitor)
    {
        var videoMode = Glfw.GetVideoMode(monitor);
        ResolutionX = videoMode.Width;
        ResolutionY = videoMode.Height;
        RefreshRate = videoMode.RefreshRate;
    }
}