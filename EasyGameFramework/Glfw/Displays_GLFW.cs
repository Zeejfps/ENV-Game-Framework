using EasyGameFramework.Api;
using GLFW;

namespace Framework.GLFW.NET;

public class Displays_GLFW : IDisplays
{
    public Displays_GLFW()
    {
        PrimaryDisplay = new Display_GLFW(Glfw.PrimaryMonitor);
    }

    public IDisplay PrimaryDisplay { get; }
}