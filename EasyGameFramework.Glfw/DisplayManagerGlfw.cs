using EasyGameFramework.Api;

namespace EasyGameFramework.Glfw;

public sealed class DisplayManagerGlfw : IDisplayManager
{
    public DisplayManagerGlfw()
    {
        PrimaryDisplay = new Display_GLFW(GLFW.Glfw.PrimaryMonitor);
    }

    public IDisplay PrimaryDisplay { get; }
}