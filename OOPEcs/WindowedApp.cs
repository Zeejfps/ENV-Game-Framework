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
}