// macOS-only app backed by GLFW (NoApi) + CAMetalLayer attached to the GLFW
// NSWindow's content view. Owns the MTLDevice, MTLCommandQueue, the run loop,
// and the per-window list.

using System.Runtime.InteropServices;
using GLFW;
using ZGF.Desktop.Backends.Glfw;
using ZGF.Rendering.Metal;
using static ZGF.Rendering.Metal.Objc;
using Monitor = GLFW.Monitor;

namespace ZGF.Desktop.Backends.Metal;

public sealed class MetalApp : IWindowedApp
{
    private readonly MetalWindow _mainWindow;
    private readonly List<IWindow> _windows = new();
    private bool _isDisposed;

    public IntPtr Device { get; }
    public IntPtr CommandQueue { get; }

    public MetalApp(StartupConfig startupConfig)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            throw new PlatformNotSupportedException("MetalApp requires macOS.");

        GLFW.Glfw.Init();
        GLFW.Glfw.DefaultWindowHints();
        GLFW.Glfw.WindowHint(Hint.ClientApi, ClientApi.None);
        GLFW.Glfw.WindowHint(Hint.Visible, false);
        if (startupConfig.IsUndecorated)
            GLFW.Glfw.WindowHint(Hint.Decorated, false);

        var window = GLFW.Glfw.CreateWindow(
            startupConfig.WindowWidth, startupConfig.WindowHeight,
            startupConfig.WindowTitle, Monitor.None, Window.None);

        Device = MetalApi.MTLCreateSystemDefaultDevice();
        if (Device == IntPtr.Zero) throw new System.Exception("MTLCreateSystemDefaultDevice returned null.");
        CommandQueue = msg_IntPtr(Device, Sel("newCommandQueue"));
        if (CommandQueue == IntPtr.Zero) throw new System.Exception("newCommandQueue returned null.");

        _mainWindow = new MetalWindow(window, Device, CommandQueue, isMain: true);
        _windows.Add(_mainWindow);
    }

    public IWindow MainWindow => _mainWindow;
    public IReadOnlyList<IWindow> Windows => _windows;
    public IReadOnlyList<MonitorWorkArea> Monitors => GlfwMonitors.WorkAreas();

    public event Action? OnTick;

    public void MakeMainContextCurrent() { /* Metal is stateless across windows */ }

    public IWindow CreatePopupWindow(in PopupWindowOptions options)
    {
        GLFW.Glfw.DefaultWindowHints();
        GLFW.Glfw.WindowHint(Hint.Visible, false);
        GLFW.Glfw.WindowHint(Hint.Decorated, false);
        GLFW.Glfw.WindowHint(Hint.Floating, true);
        GLFW.Glfw.WindowHint(Hint.FocusOnShow, false);
        GLFW.Glfw.WindowHint(Hint.Resizable, false);
        GLFW.Glfw.WindowHint(Hint.ClientApi, ClientApi.None);

        var glfw = GLFW.Glfw.CreateWindow(options.WidthPoints, options.HeightPoints, "", Monitor.None, Window.None);
        GLFW.Glfw.DefaultWindowHints();

        var popup = new MetalWindow(glfw, Device, CommandQueue, isMain: false);
        _windows.Add(popup);
        popup.OnClosed += () => _windows.Remove(popup);
        return popup;
    }

    public IWindow CreateWindow(in WindowOptions options)
    {
        GLFW.Glfw.DefaultWindowHints();
        GLFW.Glfw.WindowHint(Hint.Visible, false);
        // A real secondary window: decorated, resizable, focusable when shown — unlike the
        // borderless floating popups from CreatePopupWindow.
        GLFW.Glfw.WindowHint(Hint.Decorated, true);
        GLFW.Glfw.WindowHint(Hint.Floating, false);
        GLFW.Glfw.WindowHint(Hint.FocusOnShow, true);
        GLFW.Glfw.WindowHint(Hint.Resizable, true);
        GLFW.Glfw.WindowHint(Hint.ClientApi, ClientApi.None);

        var glfw = GLFW.Glfw.CreateWindow(options.WidthPoints, options.HeightPoints, options.Title, Monitor.None, Window.None);
        GLFW.Glfw.DefaultWindowHints();

        var window = new MetalWindow(glfw, Device, CommandQueue, isMain: false);
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
                w.RenderNow();
            }

            for (var i = _windows.Count - 1; i >= 0; i--)
            {
                if (_windows[i] is MetalWindow mw && !mw.IsMain && GLFW.Glfw.WindowShouldClose(mw.GlfwWindow))
                    GLFW.Glfw.SetWindowShouldClose(mw.GlfwWindow, false);
            }
        }
        Dispose();
        GLFW.Glfw.Terminate();
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        for (var i = _windows.Count - 1; i >= 0; i--)
        {
            if (_windows[i] != _mainWindow) _windows[i].Dispose();
        }
        _mainWindow.Dispose();
        Release(CommandQueue);
        Release(Device);
        GC.SuppressFinalize(this);
    }
}
