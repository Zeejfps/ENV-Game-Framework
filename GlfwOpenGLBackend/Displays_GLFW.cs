using ENV.Engine;
using GLFW;

namespace ENV.GLFW.NET;

public class Displays_GLFW : IDisplays
{
    public IDisplay PrimaryDisplay { get; }

    public Displays_GLFW()
    {
        PrimaryDisplay = new Display_GLFW(Glfw.PrimaryMonitor);
    }
}