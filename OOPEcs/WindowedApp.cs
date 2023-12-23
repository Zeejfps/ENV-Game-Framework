using EasyGameFramework.Api;

namespace OOPEcs;

public abstract class WindowedApp
{
    protected IWindow Window { get; }

    protected WindowedApp(IWindow window, ILogger logger)
    {
        Window = window;
    }
    
    public void Launch()
    {
        OnStartup();

        var window = Window;
        window.Title = "OOP ECS";
        window.Closed += Window_OnClosed;
        window.OpenCentered();
    }
    
    private void Window_OnClosed()
    {
        OnShutdown();
    }

    protected abstract void OnStartup();
    protected abstract void OnShutdown();
}