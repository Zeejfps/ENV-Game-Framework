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
        OnRun();
        Window.Closed += OnWindowClosed;
        EventLoop.Start();
    }

    public void Terminate()
    {
        OnTerminate();
        EventLoop.Stop();
        if (Window.IsOpened)
            Window.Close();
    }

    protected abstract void OnRun();
    protected abstract void OnTerminate();
    
    private void OnWindowClosed()
    {
        Window.Closed -= OnWindowClosed;
        Terminate();
    }
}