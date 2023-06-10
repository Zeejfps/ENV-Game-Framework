using EasyGameFramework.Api;
using Monitor = GLFW.Monitor;

namespace EasyGameFramework.Glfw;

public sealed class Display_GLFW : IDisplay
{
    public Display_GLFW(Monitor monitor)
    {
        var videoMode = GLFW.Glfw.GetVideoMode(monitor);
        ResolutionX = videoMode.Width;
        ResolutionY = videoMode.Height;
        RefreshRate = videoMode.RefreshRate;
    }

    public int ResolutionX { get; }
    public int ResolutionY { get; }
    public int RefreshRate { get; }
}