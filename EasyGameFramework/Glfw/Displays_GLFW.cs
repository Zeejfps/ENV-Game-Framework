using EasyGameFramework.Api;

namespace EasyGameFramework.Glfw;

public class Displays_GLFW : IDisplays
{
    public Displays_GLFW()
    {
        PrimaryDisplay = new Display_GLFW(GLFW.Glfw.PrimaryMonitor);
    }

    public IDisplay PrimaryDisplay { get; }
}