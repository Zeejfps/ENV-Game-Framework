using EasyGameFramework.API;
using GLFW;

namespace Framework.GLFW.NET;

public class Displays_GLFW : IDisplays
{
    public IDisplay PrimaryDisplay { get; }

    public Displays_GLFW()
    {
        PrimaryDisplay = new Display_GLFW(Glfw.PrimaryMonitor);
    }
}