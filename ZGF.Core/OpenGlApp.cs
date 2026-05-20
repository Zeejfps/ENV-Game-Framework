using GLFW;
using static GL46;
using static OpenGLSandbox.OpenGlUtils;
using Monitor = GLFW.Monitor;

namespace ZGF.Core;

public sealed class OpenGlApp : IWindowApp
{
    private readonly Window _window;
    private readonly SizeCallback _windowSizeCallback;
    private readonly SizeCallback _framebufferSizeCallback;

    private int _width;
    private int _height;
    private float _dpiScale = 1f;
    private bool _isDisposed;

    public OpenGlApp(StartupConfig startupConfig)
    {
        Glfw.Init();

        Glfw.WindowHint(Hint.ClientApi, ClientApi.OpenGL);
        // 4.1 is the highest GL version Apple ships, and macOS only honors a Core
        // context if OpenglForwardCompatible is true. Targeting 4.1+forward-compat
        // on every platform keeps a single code path that works on macOS too.
        Glfw.WindowHint(Hint.ContextVersionMajor, 4);
        Glfw.WindowHint(Hint.ContextVersionMinor, 1);
        Glfw.WindowHint(Hint.OpenglProfile, Profile.Core);
        Glfw.WindowHint(Hint.OpenglForwardCompatible, true);
        Glfw.WindowHint(Hint.Visible, false);

        if (startupConfig.IsUndecorated)
            Glfw.WindowHint(Hint.Decorated, false);

        _width = startupConfig.WindowWidth;
        _height = startupConfig.WindowHeight;
        _window = Glfw.CreateWindow(_width, _height, startupConfig.WindowTitle, Monitor.None, Window.None);

        Glfw.MakeContextCurrent(_window);
        Glfw.SwapInterval(1);

        Import(Glfw.GetProcAddress);
        AssertNoGlError();

        _dpiScale = ComputeDpiScale(_window);

        _windowSizeCallback = HandleWindowSizeChanged;
        _framebufferSizeCallback = HandleFramebufferSizeChanged;
        Glfw.SetWindowSizeCallback(_window, _windowSizeCallback);
        Glfw.SetFramebufferSizeCallback(_window, _framebufferSizeCallback);
    }

    ~OpenGlApp()
    {
        Dispose(false);
    }

    public IntPtr WindowHandle => _window;
    public Window GlfwWindow => _window;
    public int Width => _width;
    public int Height => _height;
    public float DpiScale => _dpiScale;

    public event Action? OnUpdate;
    public event Action<int, int>? OnResize;
    public event Action<int, int>? OnFramebufferResize;

    public void SwapBuffers() => Glfw.SwapBuffers(_window);

    public void Run()
    {
        var videoMode = Glfw.GetVideoMode(Glfw.PrimaryMonitor);
        var resolutionX = videoMode.Width;
        var resolutionY = videoMode.Height;

        Glfw.GetWindowSize(_window, out var windowWidth, out var windowHeight);
        var windowPosX = (int)((resolutionX - windowWidth) * 0.5f);
        var windowPosY = (int)((resolutionY - windowHeight) * 0.5f);
        Glfw.SetWindowPosition(_window, windowPosX, windowPosY);
        Glfw.ShowWindow(_window);

        while (!Glfw.WindowShouldClose(_window))
        {
            Glfw.PollEvents();
            OnUpdate?.Invoke();
            Glfw.SwapBuffers(_window);
        }
        Dispose();
        Glfw.Terminate();
    }

    private void HandleWindowSizeChanged(Window window, int width, int height)
    {
        _width = width;
        _height = height;
        OnResize?.Invoke(width, height);
    }

    private void HandleFramebufferSizeChanged(Window window, int width, int height)
    {
        _dpiScale = ComputeDpiScale(_window);
        OnFramebufferResize?.Invoke(width, height);
    }

    private static float ComputeDpiScale(Window window)
    {
        Glfw.GetFramebufferSize(window, out var fbW, out var fbH);
        Glfw.GetWindowSize(window, out var winW, out var winH);
        if (winW > 0 && winH > 0)
            return MathF.Max((float)fbW / winW, (float)fbH / winH);
        return 1f;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_isDisposed)
            return;
        _isDisposed = true;
    }
}
