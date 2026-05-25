// macOS-only app backed by GLFW (NoApi) + CAMetalLayer attached to the GLFW
// NSWindow's content view. Owns the MTLDevice, MTLCommandQueue, the run loop,
// and the per-window list.

using System.Runtime.InteropServices;
using GLFW;
using ZGF.Core.MacOs;
using static ZGF.Core.MacOs.Objc;
using Monitor = GLFW.Monitor;

namespace ZGF.Core;

public sealed class MetalApp : IApp
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

        Glfw.Init();
        Glfw.DefaultWindowHints();
        Glfw.WindowHint(Hint.ClientApi, ClientApi.None);
        Glfw.WindowHint(Hint.Visible, false);
        if (startupConfig.IsUndecorated)
            Glfw.WindowHint(Hint.Decorated, false);

        var window = Glfw.CreateWindow(
            startupConfig.WindowWidth, startupConfig.WindowHeight,
            startupConfig.WindowTitle, Monitor.None, Window.None);

        Device = Metal.MTLCreateSystemDefaultDevice();
        if (Device == IntPtr.Zero) throw new System.Exception("MTLCreateSystemDefaultDevice returned null.");
        CommandQueue = msg_IntPtr(Device, Sel("newCommandQueue"));
        if (CommandQueue == IntPtr.Zero) throw new System.Exception("newCommandQueue returned null.");

        _mainWindow = new MetalWindow(window, Device, isMain: true);
        _windows.Add(_mainWindow);
    }

    public IWindow MainWindow => _mainWindow;
    public IReadOnlyList<IWindow> Windows => _windows;

    public event Action? OnTick;

    public void MakeMainContextCurrent() { /* Metal is stateless across windows */ }

    public IWindow CreatePopupWindow(in PopupWindowOptions options)
    {
        Glfw.DefaultWindowHints();
        Glfw.WindowHint(Hint.Visible, false);
        Glfw.WindowHint(Hint.Decorated, false);
        Glfw.WindowHint(Hint.Floating, true);
        Glfw.WindowHint(Hint.FocusOnShow, false);
        Glfw.WindowHint(Hint.Resizable, false);
        Glfw.WindowHint(Hint.ClientApi, ClientApi.None);

        var glfw = Glfw.CreateWindow(options.WidthPoints, options.HeightPoints, "", Monitor.None, Window.None);
        Glfw.DefaultWindowHints();

        var popup = new MetalWindow(glfw, Device, isMain: false);
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
                if (!w.IsVisible) continue;
                if (!w.NeedsRedraw) continue;
                w.RenderNow();
                w.NeedsRedraw = false;
            }

            for (var i = _windows.Count - 1; i >= 0; i--)
            {
                if (_windows[i] is MetalWindow mw && !mw.IsMain && Glfw.WindowShouldClose(mw.GlfwWindow))
                    Glfw.SetWindowShouldClose(mw.GlfwWindow, false);
            }
        }
        Dispose();
        Glfw.Terminate();
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
