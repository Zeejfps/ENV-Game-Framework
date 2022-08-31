using EasyGameFramework.Api;
using GLFW;
using Monitor = GLFW.Monitor;

namespace Framework.GLFW.NET;

internal class Display_GLFW : IDisplay
{
    public Display_GLFW(Monitor monitor)
    {
        var videoMode = Glfw.GetVideoMode(monitor);
        ResolutionX = videoMode.Width;
        ResolutionY = videoMode.Height;
        RefreshRate = videoMode.RefreshRate;
    }

    public int ResolutionX { get; }
    public int ResolutionY { get; }
    public int RefreshRate { get; }
}