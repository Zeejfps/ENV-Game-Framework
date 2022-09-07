namespace EasyGameFramework.Api;

public abstract class WindowedApp
{
    public IWindow Window { get; }
    protected IEventLoop EventLoop { get; }
    
    private bool IsRunning { get; set; }
    
    protected WindowedApp(IWindow window, IEventLoop eventLoop)
    {
        Window = window;
        EventLoop = eventLoop;
    }
    
    public void Run()
    {
        if (IsRunning)
            return;

        IsRunning = true;
        
        var window = Window;
        window.Closed += OnWindowClosed;
        Configure(Window);
        window.OpenCentered();
        
        OnOpen();
        EventLoop.Start();
    }

    public void Close()
    {
        if (!IsRunning)
            return;

        IsRunning = false;
        OnClose();
        EventLoop.Stop();
        if (Window.IsOpened)
            Window.Close();
    }
    
    protected abstract void Configure(IWindow window);
    protected abstract void OnOpen();
    protected abstract void OnClose();
    
    private void OnWindowClosed()
    {
        Window.Closed -= OnWindowClosed;
        Close();
    }
}