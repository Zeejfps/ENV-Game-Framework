using EasyGameFramework.Api;
using Tetris;

namespace OOPEcs;

public abstract class WindowedApp : World
{
    private readonly IWindow m_Window;
    
    protected WindowedApp(IWindow window)
    {
        m_Window = window;
    }
    
    public void Launch()
    {
        Load();

        var window = m_Window;
        window.Title = "OOP ECS";
        window.Closed += Window_OnClosed;
        window.OpenCentered();
    }
    
    private void Window_OnClosed()
    {
        Unload();
    }
}