using GLFW;
using static GL46;
using static OpenGLSandbox.OpenGlUtils;
using Monitor = GLFW.Monitor;

namespace ZGF.GlfwUtils;

public readonly struct StartupConfig
{
    public required int WindowWidth { get; init; }
    public required int WindowHeight { get; init; }
    public required string WindowTitle { get; init; }
}

public abstract class OpenGlApp : IDisposable
{
    protected readonly Window WindowHandle;
    
    protected bool _isDisposed;
    
    protected OpenGlApp(StartupConfig startupConfig)
    {
        Glfw.Init();

        Glfw.WindowHint(Hint.ClientApi, ClientApi.OpenGL);
        Glfw.WindowHint(Hint.ContextVersionMajor, 4);
        Glfw.WindowHint(Hint.OpenglProfile, Profile.Core);

        var windowWidth = startupConfig.WindowWidth;
        var windowHeight = startupConfig.WindowHeight;
        var windowTitle =  startupConfig.WindowTitle;
        var windowHandle = Glfw.CreateWindow(windowWidth, windowHeight, windowTitle, Monitor.None, Window.None);
        WindowHandle = windowHandle;
        
        Glfw.MakeContextCurrent(windowHandle);
        Glfw.ShowWindow(windowHandle);
        Glfw.SwapInterval(1);
        
        Import(Glfw.GetProcAddress);
        AssertNoGlError();
    }

    ~OpenGlApp()
    {
        Dispose(false);
    }
    
    public void Run()
    {
        Glfw.ShowWindow(WindowHandle);
        while (!Glfw.WindowShouldClose(WindowHandle))
        {
            Glfw.PollEvents();
            OnUpdate();
            Glfw.SwapBuffers(WindowHandle);
        }
        DisposeManagedResources();
        Glfw.Terminate();
    }

    protected abstract void OnUpdate();

    protected abstract void DisposeManagedResources();
    protected abstract void DisposeUnmanagedResources();

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_isDisposed)
            return;
        
        if (disposing)
        {
            DisposeManagedResources();
        }

        DisposeUnmanagedResources();
        _isDisposed = true;
    }
}