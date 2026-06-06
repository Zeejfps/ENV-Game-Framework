using GLFW;
using ZGF.Desktop.Backends.Glfw;
using static GL46;
using static OpenGLSandbox.OpenGlUtils;
using Monitor = GLFW.Monitor;

namespace ZGF.Desktop.Backends.OpenGl;

public sealed class OpenGlApp : IWindowedApp
{
    private readonly OpenGlWindow _mainWindow;
    private readonly List<OpenGlWindow> _windows = new();
    private bool _isDisposed;

    public OpenGlApp(StartupConfig startupConfig)
    {
        GLFW.Glfw.Init();

        GLFW.Glfw.DefaultWindowHints();
        GLFW.Glfw.WindowHint(Hint.ClientApi, ClientApi.OpenGL);
        GLFW.Glfw.WindowHint(Hint.ContextVersionMajor, 4);
        GLFW.Glfw.WindowHint(Hint.ContextVersionMinor, 1);
        GLFW.Glfw.WindowHint(Hint.OpenglProfile, Profile.Core);
        GLFW.Glfw.WindowHint(Hint.OpenglForwardCompatible, true);
        GLFW.Glfw.WindowHint(Hint.Visible, false);

        if (startupConfig.IsUndecorated)
            GLFW.Glfw.WindowHint(Hint.Decorated, false);

        var window = GLFW.Glfw.CreateWindow(
            startupConfig.WindowWidth, startupConfig.WindowHeight,
            startupConfig.WindowTitle, Monitor.None, Window.None);

        GLFW.Glfw.MakeContextCurrent(window);
        GLFW.Glfw.SwapInterval(1);
        Import(GLFW.Glfw.GetProcAddress);
        AssertNoGlError();

        _mainWindow = new OpenGlWindow(window, isMain: true);
        _windows.Add(_mainWindow);
    }

    public IWindow MainWindow => _mainWindow;
    public IReadOnlyList<IWindow> Windows => _windows;

    public IReadOnlyList<MonitorWorkArea> Monitors => GlfwMonitors.WorkAreas();

    public event Action? OnTick;

    public void MakeMainContextCurrent()
    {
        GLFW.Glfw.MakeContextCurrent(_mainWindow.GlfwWindow);
    }

    public IWindow CreatePopupWindow(in PopupWindowOptions options)
    {
        GLFW.Glfw.DefaultWindowHints();
        GLFW.Glfw.WindowHint(Hint.Visible, false);
        GLFW.Glfw.WindowHint(Hint.Decorated, false);
        GLFW.Glfw.WindowHint(Hint.Floating, true);
        GLFW.Glfw.WindowHint(Hint.FocusOnShow, false);
        GLFW.Glfw.WindowHint(Hint.Resizable, false);
        GLFW.Glfw.WindowHint(Hint.ClientApi, ClientApi.OpenGL);
        GLFW.Glfw.WindowHint(Hint.ContextVersionMajor, 4);
        GLFW.Glfw.WindowHint(Hint.ContextVersionMinor, 1);
        GLFW.Glfw.WindowHint(Hint.OpenglProfile, Profile.Core);
        GLFW.Glfw.WindowHint(Hint.OpenglForwardCompatible, true);

        var glfw = GLFW.Glfw.CreateWindow(
            options.WidthPoints, options.HeightPoints,
            "", Monitor.None, _mainWindow.GlfwWindow);

        GLFW.Glfw.DefaultWindowHints();

        GLFW.Glfw.MakeContextCurrent(glfw);
        // Popups must not gate vsync — each SwapBuffers on each popup context
        // serializes one vblank wait, so with vsync on N popups the loop
        // becomes refresh / (1 + N). Only the main window paces vsync.
        GLFW.Glfw.SwapInterval(0);

        var popup = new OpenGlWindow(glfw, isMain: false);
        _windows.Add(popup);
        popup.OnClosed += () => _windows.Remove(popup);
        return popup;
    }

    public IWindow CreateWindow(in WindowOptions options)
    {
        GLFW.Glfw.DefaultWindowHints();
        GLFW.Glfw.WindowHint(Hint.Visible, false);
        // A real secondary window: decorated, resizable, and able to take focus when shown,
        // unlike CreatePopupWindow's borderless floating popups.
        GLFW.Glfw.WindowHint(Hint.Decorated, true);
        GLFW.Glfw.WindowHint(Hint.Floating, false);
        GLFW.Glfw.WindowHint(Hint.FocusOnShow, true);
        GLFW.Glfw.WindowHint(Hint.Resizable, true);
        GLFW.Glfw.WindowHint(Hint.ClientApi, ClientApi.OpenGL);
        GLFW.Glfw.WindowHint(Hint.ContextVersionMajor, 4);
        GLFW.Glfw.WindowHint(Hint.ContextVersionMinor, 1);
        GLFW.Glfw.WindowHint(Hint.OpenglProfile, Profile.Core);
        GLFW.Glfw.WindowHint(Hint.OpenglForwardCompatible, true);

        // Share the GL context with the main window so the shared font atlas / textures
        // (GlSharedResources) are visible to this window's canvas.
        var glfw = GLFW.Glfw.CreateWindow(
            options.WidthPoints, options.HeightPoints,
            options.Title, Monitor.None, _mainWindow.GlfwWindow);

        GLFW.Glfw.DefaultWindowHints();

        GLFW.Glfw.MakeContextCurrent(glfw);
        // Like popups, secondary windows must not gate vsync — only the main window paces it.
        GLFW.Glfw.SwapInterval(0);

        var window = new OpenGlWindow(glfw, isMain: false);
        _windows.Add(window);
        window.OnClosed += () => _windows.Remove(window);
        return window;
    }

    public void Run()
    {
        var videoMode = GLFW.Glfw.GetVideoMode(GLFW.Glfw.PrimaryMonitor);
        GLFW.Glfw.GetWindowSize(_mainWindow.GlfwWindow, out var ww, out var wh);
        var px = (int)((videoMode.Width - ww) * 0.5f);
        var py = (int)((videoMode.Height - wh) * 0.5f);
        GLFW.Glfw.SetWindowPosition(_mainWindow.GlfwWindow, px, py);
        _mainWindow.Show();

        while (!GLFW.Glfw.WindowShouldClose(_mainWindow.GlfwWindow))
        {
            GLFW.Glfw.PollEvents();
            OnTick?.Invoke();

            _mainWindow.RequestRedraw();
            for (var i = 0; i < _windows.Count; i++)
            {
                var w = _windows[i];
                if (!w.IsVisible) continue;
                if (!w.NeedsRedraw) continue;
                w.MakeContextCurrent();
                w.RenderNow();
            }

            // Popups asked to close (e.g., WM_CLOSE) get their flag reset; factory handles release.
            for (var i = _windows.Count - 1; i >= 0; i--)
            {
                var ogw = _windows[i];
                if (!ogw.IsMain && GLFW.Glfw.WindowShouldClose(ogw.GlfwWindow))
                {
                    GLFW.Glfw.SetWindowShouldClose(ogw.GlfwWindow, false);
                }
            }
        }
        Dispose();
        GLFW.Glfw.Terminate();
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        // Dispose popups first, then main.
        for (var i = _windows.Count - 1; i >= 0; i--)
        {
            if (_windows[i] != _mainWindow) _windows[i].Dispose();
        }
        _mainWindow.Dispose();
        GC.SuppressFinalize(this);
    }
}
