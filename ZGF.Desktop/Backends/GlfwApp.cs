using GLFW;
using Monitor = GLFW.Monitor;

namespace ZGF.Desktop.Backends;

// Shared GLFW-backed IWindowedApp plumbing: the main window's creation, the run loop, the
// per-window list and teardown. Backends (OpenGL, Metal) subclass this and fill only the
// seams that differ — the client API hints, how a secondary window is opened and wrapped,
// per-turn bracketing, and GPU resource release.
public abstract class GlfwApp<TWindow> : IWindowedApp where TWindow : GlfwWindowBase
{
    private const double IdleEventTimeoutSeconds = 0.1;

    private readonly List<TWindow> _windows = new();
    private readonly AppForegroundTracker _foreground;
    private readonly StartupConfig _startupConfig;
    private readonly ClientApi _clientApi;
    private bool _isDisposed;

    protected GlfwApp(StartupConfig startupConfig, ClientApi clientApi, Func<Window, TWindow> wrapMainWindow)
    {
        _startupConfig = startupConfig;
        _clientApi = clientApi;
        Glfw.Init();

        Glfw.DefaultWindowHints();
        ApplyClientApiHints();
        Glfw.WindowHint(Hint.Visible, false);

        if (startupConfig.IsUndecorated)
            Glfw.WindowHint(Hint.Decorated, false);

        if (startupConfig.StartUnfocused)
        {
            Glfw.WindowHint(Hint.FocusOnShow, false);
            Glfw.WindowHint(Hint.Focused, false);
        }

        var window = Glfw.CreateWindow(
            startupConfig.WindowWidth, startupConfig.WindowHeight,
            startupConfig.WindowTitle, Monitor.None, Window.None);

        Main = wrapMainWindow(window);
        _windows.Add(Main);
        _foreground = new AppForegroundTracker(_windows);
        _foreground.Watch(Main);
    }

    protected TWindow Main { get; }

    public IWindow MainWindow => Main;
    public IReadOnlyList<IWindow> Windows => _windows;
    public IReadOnlyList<MonitorWorkArea> Monitors => GlfwMonitors.WorkAreas();

    public bool IsForeground => _foreground.IsForeground;

    public event Action<bool> OnForegroundChanged
    {
        add => _foreground.Changed += value;
        remove => _foreground.Changed -= value;
    }

    public event Action? OnTick;

    public void Wake() => Glfw.PostEmptyEvent();

    public void Quit()
    {
        Glfw.SetWindowShouldClose(Main.GlfwWindow, true);
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
        ApplyClientApiHints();
        // Popups paint their own silhouette — rounded corners, borders, shadows — over a
        // framebuffer cleared to transparent. Without transparency the compositor discards
        // the alpha channel and the pixels outside that silhouette show as opaque black.
        return Track(OpenWindow(options.WidthPoints, options.HeightPoints, "", transparent: true));
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
        ApplyClientApiHints();
        return Track(OpenWindow(options.WidthPoints, options.HeightPoints, options.Title, transparent: false));
    }

    // Creates the GLFW window for a popup or secondary window once the shared hints above are
    // set, applies any backend-specific hints or post-creation setup, and wraps it. Must leave
    // the hint state reset (Glfw.DefaultWindowHints) before returning.
    protected abstract TWindow OpenWindow(int widthPoints, int heightPoints, string title, bool transparent);

    protected virtual void BeginTurn() { }
    protected virtual void EndTurn() { }

    // Runs after every window has been disposed and before GLFW terminates.
    protected virtual void ReleaseDevice() { }

    private void ApplyClientApiHints()
    {
        Glfw.WindowHint(Hint.ClientApi, _clientApi);
        if (_clientApi != ClientApi.OpenGL) return;
        Glfw.WindowHint(Hint.ContextVersionMajor, 4);
        Glfw.WindowHint(Hint.ContextVersionMinor, 1);
        Glfw.WindowHint(Hint.OpenglProfile, Profile.Core);
        Glfw.WindowHint(Hint.OpenglForwardCompatible, true);
    }

    private TWindow Track(TWindow window)
    {
        _windows.Add(window);
        _foreground.Watch(window);
        window.OnClosed += () => _windows.Remove(window);
        return window;
    }

    public void Run()
    {
        Glfw.GetWindowSize(Main.GlfwWindow, out var ww, out var wh);
        var (px, py) = WindowPlacement.Compute(
            Monitors, ww, wh, _startupConfig.WindowX, _startupConfig.WindowY);
        Glfw.SetWindowPosition(Main.GlfwWindow, px, py);
        Main.Show();

        while (!Glfw.WindowShouldClose(Main.GlfwWindow))
        {
            BeginTurn();
            try
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
                    var w = _windows[i];
                    if (!w.IsMain && Glfw.WindowShouldClose(w.GlfwWindow))
                        Glfw.SetWindowShouldClose(w.GlfwWindow, false);
                }

                // Nothing painted: block for OS events instead of spinning. The timeout bounds
                // staleness for time-based housekeeping nothing wakes us for; when we did paint,
                // the main window's vsync'd SwapBuffers paces the loop.
                if (!anyRendered)
                    Glfw.WaitEventsTimeout(IdleEventTimeoutSeconds);
            }
            finally
            {
                EndTurn();
            }
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
            if (_windows[i] != Main) _windows[i].Dispose();
        }
        Main.Dispose();
        ReleaseDevice();
        Glfw.Terminate();
        GC.SuppressFinalize(this);
    }
}
