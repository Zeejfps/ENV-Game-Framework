using GLFW;
using static GL46;
using static OpenGLSandbox.OpenGlUtils;
using Monitor = GLFW.Monitor;

namespace ZGF.Core;

public sealed class OpenGlApp : IApp
{
    private readonly OpenGlWindow _mainWindow;
    private readonly List<IWindow> _windows = new();
    private bool _isDisposed;

    public OpenGlApp(StartupConfig startupConfig)
    {
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
    public Window MainGlfwWindow => _mainWindow.GlfwWindow;

    public event Action? OnTick;

    public void MakeMainContextCurrent()
    {
        Glfw.MakeContextCurrent(_mainWindow.GlfwWindow);
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
        Glfw.SwapInterval(1);

        var popup = new OpenGlWindow(glfw, isMain: false);
        _windows.Add(popup);
        popup.OnClosed += () => _windows.Remove(popup);
        return popup;
    }

    public void Run()
    {
        var videoMode = Glfw.GetVideoMode(Glfw.PrimaryMonitor);
        Glfw.GetWindowSize(_mainWindow.GlfwWindow, out var ww, out var wh);
        var px = (int)((videoMode.Width - ww) * 0.5f);
        var py = (int)((videoMode.Height - wh) * 0.5f);
        Glfw.SetWindowPosition(_mainWindow.GlfwWindow, px, py);
        _mainWindow.Show();

        while (!Glfw.WindowShouldClose(_mainWindow.GlfwWindow))
        {
            Glfw.PollEvents();
            OnTick?.Invoke();

            _mainWindow.NeedsRedraw = true;
            for (var i = 0; i < _windows.Count; i++)
            {
                var w = _windows[i];
                var isPopup = !(w is OpenGlWindow ogw && ogw.IsMain);
                if (!w.IsVisible)
                {
                    if (isPopup)
                        Console.Error.WriteLine($"[popup:Lifecycle] MainLoop: skip hwnd={w.WindowHandle.ToInt64():X} reason=!IsVisible NeedsRedraw={w.NeedsRedraw}");
                    continue;
                }
                if (!w.NeedsRedraw)
                {
                    if (isPopup)
                        Console.Error.WriteLine($"[popup:Lifecycle] MainLoop: skip hwnd={w.WindowHandle.ToInt64():X} reason=!NeedsRedraw IsVisible=true");
                    continue;
                }
                if (isPopup)
                    Console.Error.WriteLine($"[popup:Lifecycle] MainLoop: render hwnd={w.WindowHandle.ToInt64():X} IsVisible=true NeedsRedraw=true");
                w.MakeContextCurrent();
                w.RenderNow();
                w.NeedsRedraw = false;
            }

            // Popups asked to close (e.g., WM_CLOSE) get their flag reset; factory handles release.
            for (var i = _windows.Count - 1; i >= 0; i--)
            {
                if (_windows[i] is OpenGlWindow ogw && !ogw.IsMain && Glfw.WindowShouldClose(ogw.GlfwWindow))
                {
                    Glfw.SetWindowShouldClose(ogw.GlfwWindow, false);
                }
            }
        }
        Dispose();
        Glfw.Terminate();
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
