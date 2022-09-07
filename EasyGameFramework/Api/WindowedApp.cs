namespace EasyGameFramework.Api;

public abstract class WindowedApp
{
    public IWindow Window { get; }
    
    protected IEventLoop EventLoop { get; }
    
    protected WindowedApp(IWindow window, IEventLoop eventLoop)
    {
        Window = window;
        EventLoop = eventLoop;
    }
    
    public void Run()
    {
        var window = Window;
        window.Closed += OnWindowClosed;
        Configure(Window);
        window.OpenCentered();
        
        Start();
        EventLoop.Start();
    }

    public void Terminate()
    {
        Stop();
        EventLoop.Stop();
        if (Window.IsOpened)
            Window.Close();
    }
    
    protected abstract void Configure(IWindow window);
    protected abstract void Start();
    protected abstract void Stop();
    
    private void OnWindowClosed()
    {
        Window.Closed -= OnWindowClosed;
        Terminate();
    }
}