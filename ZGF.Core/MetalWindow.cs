using GLFW;
using ZGF.Core.MacOs;
using static ZGF.Core.MacOs.Objc;

namespace ZGF.Core;

public sealed class MetalWindow : IWindow
{
    private readonly Window _window;
    private readonly bool _isMain;
    private readonly SizeCallback _windowSizeCallback;
    private readonly SizeCallback _framebufferSizeCallback;
    private readonly FocusCallback _focusCallback;
    private readonly WindowCallback _closeCallback;

    private int _width;
    private int _height;
    private float _dpiScale = 1f;
    private bool _isVisible;
    private bool _isDisposed;

    public Window GlfwWindow => _window;
    public IntPtr Layer { get; }
    public IntPtr NsWindow { get; }
    public bool IsMain => _isMain;

    public Action? RenderFrame { get; set; }
    public event Action? OnClosed;

    public MetalWindow(Window window, IntPtr device, bool isMain)
    {
        _window = window;
        _isMain = isMain;
        NsWindow = Native.GetCocoaWindow(window);

        Glfw.GetWindowSize(window, out _width, out _height);
        Glfw.GetFramebufferSize(window, out var fbW, out var fbH);
        _dpiScale = ComputeDpiScale(window, fbW, fbH);
        Layer = AttachMetalLayer(window, device, fbW, fbH);

        _isVisible = isMain ? false : false;

        _windowSizeCallback = HandleWindowSizeChanged;
        _framebufferSizeCallback = HandleFramebufferSizeChanged;
        _focusCallback = HandleFocusChanged;
        _closeCallback = HandleClose;
        Glfw.SetWindowSizeCallback(window, _windowSizeCallback);
        Glfw.SetFramebufferSizeCallback(window, _framebufferSizeCallback);
        Glfw.SetWindowFocusCallback(window, _focusCallback);
        Glfw.SetCloseCallback(window, _closeCallback);
    }

    public IntPtr WindowHandle => _window;
    public int Width => _width;
    public int Height => _height;
    public float DpiScale => _dpiScale;
    public bool IsVisible => _isVisible;
    public bool NeedsRedraw { get; set; } = true;

    public event Action<int, int>? OnResize;
    public event Action<int, int>? OnFramebufferResize;
    public event Action<bool>? OnFocusChanged;
    public event Action? OnClose;

    public void Show() { Glfw.ShowWindow(_window); _isVisible = true; NeedsRedraw = true; }
    public void Hide() { Glfw.HideWindow(_window); _isVisible = false; }
    public void Focus() => Glfw.FocusWindow(_window);
    public void SetPosition(int x, int y) => Glfw.SetWindowPosition(_window, x, y);
    public void SetSize(int w, int h) => Glfw.SetWindowSize(_window, w, h);
    public void RequestRedraw() => NeedsRedraw = true;
    public void MakeContextCurrent() { /* Metal is stateless across windows */ }

    public void RenderNow() => RenderFrame?.Invoke();

    private void HandleWindowSizeChanged(Window window, int width, int height)
    {
        _width = width;
        _height = height;
        Glfw.GetFramebufferSize(_window, out var fbW, out var fbH);
        SetDrawableSize(Layer, fbW, fbH);
        OnResize?.Invoke(width, height);
    }

    private void HandleFramebufferSizeChanged(Window window, int width, int height)
    {
        SetDrawableSize(Layer, width, height);
        _dpiScale = ComputeDpiScale(_window, width, height);
        OnFramebufferResize?.Invoke(width, height);
    }

    private void HandleFocusChanged(Window window, bool focused) => OnFocusChanged?.Invoke(focused);
    private void HandleClose(Window window) => OnClose?.Invoke();

    private static float ComputeDpiScale(Window window, int fbWidth, int fbHeight)
    {
        var nsWindow = Native.GetCocoaWindow(window);
        if (nsWindow != IntPtr.Zero)
        {
            var s = (float)msg_Double(nsWindow, Sel("backingScaleFactor"));
            if (s > 0f) return s;
        }
        Glfw.GetWindowSize(window, out var winW, out var winH);
        if (winW > 0 && winH > 0)
            return MathF.Max((float)fbWidth / winW, (float)fbHeight / winH);
        return 1f;
    }

    private static IntPtr AttachMetalLayer(Window glfwWindow, IntPtr device, int fbWidth, int fbHeight)
    {
        var nsWindow = Native.GetCocoaWindow(glfwWindow);
        if (nsWindow == IntPtr.Zero) throw new System.Exception("glfwGetCocoaWindow returned null.");

        var contentView = msg_IntPtr(nsWindow, Sel("contentView"));
        if (contentView == IntPtr.Zero) throw new System.Exception("NSWindow contentView is null.");

        var caMetalLayerClass = Class("CAMetalLayer");
        if (caMetalLayerClass == IntPtr.Zero) throw new System.Exception("CAMetalLayer class not found.");

        var layer = msg_IntPtr(caMetalLayerClass, Sel("layer"));
        if (layer == IntPtr.Zero) throw new System.Exception("CAMetalLayer layer factory returned null.");

        msg_Void_IntPtr(layer, Sel("setDevice:"), device);
        msg_Void_UInt(layer, Sel("setPixelFormat:"), (uint)MTLPixelFormat.BGRA8Unorm);
        msg_Void_Bool(layer, Sel("setFramebufferOnly:"), true);

        var backingScale = msg_Double(nsWindow, Sel("backingScaleFactor"));
        msg_Void_Double(layer, Sel("setContentsScale:"), backingScale);

        SetDrawableSize(layer, fbWidth, fbHeight);

        msg_Void_IntPtr(contentView, Sel("setLayer:"), layer);
        msg_Void_Bool(contentView, Sel("setWantsLayer:"), true);

        return layer;
    }

    private static void SetDrawableSize(IntPtr layer, int width, int height)
    {
        msg_Void_CGSize(layer, Sel("setDrawableSize:"), new CGSize(width, height));
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        if (!_isMain)
        {
            Glfw.DestroyWindow(_window);
            OnClosed?.Invoke();
        }
    }
}
