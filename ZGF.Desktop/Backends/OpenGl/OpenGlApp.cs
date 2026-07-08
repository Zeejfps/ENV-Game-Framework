using GLFW;
using static GL46;
using static OpenGLSandbox.OpenGlUtils;
using Monitor = GLFW.Monitor;

namespace ZGF.Desktop.Backends.OpenGl;

public sealed class OpenGlApp : IWindowedApp
{
    private const double IdleEventTimeoutSeconds = 0.1;

    private readonly OpenGlWindow _mainWindow;
    private readonly List<OpenGlWindow> _windows = new();
    private readonly StartupConfig _startupConfig;
    private bool _isDisposed;

    public OpenGlApp(StartupConfig startupConfig)
    {
        _startupConfig = startupConfig;
        Glfw.Init();

        Glfw.DefaultWindowHints();
        Glfw.WindowHint(Hint.ClientApi, ClientApi.OpenGL);
        Glfw.WindowHint(Hint.ContextVersionMajor, 4);
        Glfw.WindowHint(Hint.ContextVersionMinor, 1);
        Glfw.WindowHint(Hint.OpenglProfile, Profile.Core);
        Glfw.WindowHint(Hint.OpenglForwardCompatible, true);
        Glfw.WindowHint(Hint.Visible, false);

        if (startupConfig.IsUndecorated)
            Glfw.WindowHint(Hint.Decorated, false);

        var window = Glfw.CreateWindow(
            startupConfig.WindowWidth, startupConfig.WindowHeight,
            startupConfig.WindowTitle, Monitor.None, Window.None);

        Glfw.MakeContextCurrent(window);
        Glfw.SwapInterval(1);
        Import(Glfw.GetProcAddress);
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
        Glfw.MakeContextCurrent(_mainWindow.GlfwWindow);
    }

    public void Wake() => Glfw.PostEmptyEvent();

    public void Quit()
    {
        Glfw.SetWindowShouldClose(_mainWindow.GlfwWindow, true);
        Wake();
    }

    public IWindow CreatePopupWindow(in PopupWindowOptions options)
    {
        Glfw.DefaultWindowHints();
        Glfw.WindowHint(Hint.Visible, false);
        Glfw.WindowHint(Hint.Decorated, false);
        Glfw.WindowHint(Hint.Floating, true);
        Glfw.WindowHint(Hint.FocusOnShow, false);
        Glfw.WindowHint(Hint.Resizable, false);
        Glfw.WindowHint(Hint.ClientApi, ClientApi.OpenGL);
        Glfw.WindowHint(Hint.ContextVersionMajor, 4);
        Glfw.WindowHint(Hint.ContextVersionMinor, 1);
        Glfw.WindowHint(Hint.OpenglProfile, Profile.Core);
        Glfw.WindowHint(Hint.OpenglForwardCompatible, true);

        var glfw = Glfw.CreateWindow(
            options.WidthPoints, options.HeightPoints,
            "", Monitor.None, _mainWindow.GlfwWindow);

        Glfw.DefaultWindowHints();

        Glfw.MakeContextCurrent(glfw);
        // Popups must not gate vsync — each SwapBuffers on each popup context
        // serializes one vblank wait, so with vsync on N popups the loop
        // becomes refresh / (1 + N). Only the main window paces vsync.
        Glfw.SwapInterval(0);

        var popup = new OpenGlWindow(glfw, isMain: false);
        _windows.Add(popup);
        popup.OnClosed += () => _windows.Remove(popup);
        return popup;
    }

    public IWindow CreateWindow(in WindowOptions options)
    {
        Glfw.DefaultWindowHints();
        Glfw.WindowHint(Hint.Visible, false);
        // A real secondary window: decorated, resizable, and able to take focus when shown,
        // unlike CreatePopupWindow's borderless floating popups.
        Glfw.WindowHint(Hint.Decorated, true);
        Glfw.WindowHint(Hint.Floating, false);
        Glfw.WindowHint(Hint.FocusOnShow, true);
        Glfw.WindowHint(Hint.Resizable, true);
        Glfw.WindowHint(Hint.ClientApi, ClientApi.OpenGL);
        Glfw.WindowHint(Hint.ContextVersionMajor, 4);
        Glfw.WindowHint(Hint.ContextVersionMinor, 1);
        Glfw.WindowHint(Hint.OpenglProfile, Profile.Core);
        Glfw.WindowHint(Hint.OpenglForwardCompatible, true);

        // Share the GL context with the main window so the shared font atlas / textures
        // (GlSharedResources) are visible to this window's canvas.
        var glfw = Glfw.CreateWindow(
            options.WidthPoints, options.HeightPoints,
            options.Title, Monitor.None, _mainWindow.GlfwWindow);

        Glfw.DefaultWindowHints();

        Glfw.MakeContextCurrent(glfw);
        // Like popups, secondary windows must not gate vsync — only the main window paces it.
        Glfw.SwapInterval(0);

        var window = new OpenGlWindow(glfw, isMain: false);
        _windows.Add(window);
        window.OnClosed += () => _windows.Remove(window);
        return window;
    }

    public void Run()
    {
        Glfw.GetWindowSize(_mainWindow.GlfwWindow, out var ww, out var wh);
        var (px, py) = WindowPlacement.Compute(
            Monitors, ww, wh, _startupConfig.WindowX, _startupConfig.WindowY);
        Glfw.SetWindowPosition(_mainWindow.GlfwWindow, px, py);
        _mainWindow.Show();

        while (!Glfw.WindowShouldClose(_mainWindow.GlfwWindow))
        {
            Glfw.PollEvents();
            OnTick?.Invoke();

            var anyRendered = false;
            for (var i = 0; i < _windows.Count; i++)
            {
                var w = _windows[i];
                if (!w.IsVisible) continue;
                if (!w.NeedsRedraw) continue;
                w.MakeContextCurrent();
                w.RenderNow();
                anyRendered = true;
            }

            // Popups asked to close (e.g., WM_CLOSE) get their flag reset; factory handles release.
            for (var i = _windows.Count - 1; i >= 0; i--)
            {
                var ogw = _windows[i];
                if (!ogw.IsMain && Glfw.WindowShouldClose(ogw.GlfwWindow))
                {
                    Glfw.SetWindowShouldClose(ogw.GlfwWindow, false);
                }
            }

            // Nothing painted: block for OS events instead of spinning. The timeout bounds
            // staleness for time-based housekeeping nothing wakes us for; when we did paint,
            // the main window's vsync'd SwapBuffers paces the loop.
            if (!anyRendered)
                Glfw.WaitEventsTimeout(IdleEventTimeoutSeconds);
        }
        // The run loop exiting does NOT tear anything down: the owner (e.g. GuiApp) disposes this
        // app after Run() returns, and its teardown of secondary windows / popups / the render
        // backend still needs GLFW alive and contexts makeable. Terminating here would pull GLFW
        // out from under that teardown ("GLFW library is not initialized"). Terminate() runs in
        // Dispose(), which the owner calls last.
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
        Glfw.Terminate();
        GC.SuppressFinalize(this);
    }
}
