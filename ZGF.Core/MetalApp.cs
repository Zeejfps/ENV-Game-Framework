// macOS-only window app backed by GLFW (NoApi) + CAMetalLayer attached to
// the GLFW NSWindow's content view. Exposes a Metal device + command queue
// and a per-frame drawable acquisition pattern. Mirrors OpenGlApp's
// IWindowApp surface; Mac builds resolve to this via PlatformBackend.

using System.Runtime.InteropServices;
using GLFW;
using ZGF.Core.MacOs;
using static ZGF.Core.MacOs.Objc;
using Monitor = GLFW.Monitor;

namespace ZGF.Core;

public sealed class MetalApp : IWindowApp
{
    private readonly Window _window;
    private readonly SizeCallback _windowSizeCallback;
    private readonly SizeCallback _framebufferSizeCallback;

    public IntPtr Device { get; }
    public IntPtr CommandQueue { get; }
    public IntPtr Layer { get; }

    private int _width;
    private int _height;
    private float _dpiScale = 1f;
    private bool _isDisposed;

    public MetalApp(StartupConfig startupConfig)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            throw new PlatformNotSupportedException("MetalApp requires macOS.");

        Glfw.Init();
        Glfw.WindowHint(Hint.ClientApi, ClientApi.None);
        Glfw.WindowHint(Hint.Visible, false);
        if (startupConfig.IsUndecorated)
            Glfw.WindowHint(Hint.Decorated, false);

        _width = startupConfig.WindowWidth;
        _height = startupConfig.WindowHeight;
        _window = Glfw.CreateWindow(_width, _height, startupConfig.WindowTitle, Monitor.None, Window.None);

        Device = Metal.MTLCreateSystemDefaultDevice();
        if (Device == IntPtr.Zero) throw new System.Exception("MTLCreateSystemDefaultDevice returned null.");

        CommandQueue = msg_IntPtr(Device, Sel("newCommandQueue"));
        if (CommandQueue == IntPtr.Zero) throw new System.Exception("newCommandQueue returned null.");

        // CAMetalLayer drawableSize must be in framebuffer pixels — twice the
        // window's point size on Retina.
        Glfw.GetFramebufferSize(_window, out var fbW, out var fbH);
        _dpiScale = ComputeDpiScale(_window, fbW, fbH);
        Layer = AttachMetalLayer(_window, Device, fbW, fbH);

        _windowSizeCallback = HandleWindowSizeChanged;
        _framebufferSizeCallback = HandleFramebufferSizeChanged;
        Glfw.SetWindowSizeCallback(_window, _windowSizeCallback);
        Glfw.SetFramebufferSizeCallback(_window, _framebufferSizeCallback);
    }

    ~MetalApp()
    {
        Dispose(false);
    }

    public IntPtr WindowHandle => _window;
    public int Width => _width;
    public int Height => _height;
    public float DpiScale => _dpiScale;

    public event Action? OnUpdate;
    public event Action<int, int>? OnResize;
    public event Action<int, int>? OnFramebufferResize;

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
            // Metal presents via cmdBuffer.PresentDrawable in the per-frame
            // render callback; no SwapBuffers equivalent at this level.
        }
        Dispose();
        Glfw.Terminate();
    }

    private void HandleWindowSizeChanged(Window window, int width, int height)
    {
        _width = width;
        _height = height;
        // CAMetalLayer drawable size is in framebuffer pixels (account for HiDPI scale).
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

        // contentsScale = window.backingScaleFactor so CAMetalLayer's point-vs-pixel
        // math lines up with our drawableSize (which is in pixels).
        var backingScale = msg_Double(nsWindow, Sel("backingScaleFactor"));
        msg_Void_Double(layer, Sel("setContentsScale:"), backingScale);

        SetDrawableSize(layer, fbWidth, fbHeight);

        // setLayer: BEFORE setWantsLayer: so the view adopts our layer as its
        // backing layer (and AppKit then keeps the layer's frame in sync with
        // the view's bounds). Reversed order replaces the auto-created backing
        // layer and may leave us with frame (0,0,0,0).
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
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_isDisposed) return;
        _isDisposed = true;
        // CAMetalLayer is owned by NSView; do not release ourselves.
        Release(CommandQueue);
        Release(Device);
    }
}
