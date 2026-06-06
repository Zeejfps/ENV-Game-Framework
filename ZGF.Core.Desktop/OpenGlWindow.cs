using System.Runtime.InteropServices;
using GLFW;
using ZGF.KeyboardModule;
using ZGF.KeyboardModule.GlfwAdapter;

namespace ZGF.Core;

public sealed class OpenGlWindow : IWindow
{
    private readonly Window _window;
    private readonly IntPtr _nativeHandle;
    private readonly bool _isMain;
    private readonly SizeCallback _windowSizeCallback;
    private readonly SizeCallback _framebufferSizeCallback;
    private readonly FocusCallback _focusCallback;
    private readonly WindowCallback _closeCallback;
    private readonly KeyCallback _keyCallback;
    private readonly MouseButtonCallback _mouseButtonCallback;
    private readonly MouseCallback _scrollCallback;
    private readonly MouseEnterCallback _cursorEnterCallback;

    private int _width;
    private int _height;
    private float _dpiScale = 1f;
    private bool _isVisible;
    private bool _isDisposed;

    public Window GlfwWindow => _window;
    public bool IsMain => _isMain;

    public Action? RenderFrame { get; set; }
    public event Action? OnClosed;

    public OpenGlWindow(Window window, bool isMain)
    {
        _window = window;
        _nativeHandle = ComputeNativeHandle(window);
        _isMain = isMain;
        Glfw.GetWindowSize(window, out _width, out _height);
        _dpiScale = ComputeDpiScale(window);
        _isVisible = isMain
            ? Glfw.GetWindowAttribute(window, WindowAttribute.Visible)
            : false;

        _windowSizeCallback = HandleWindowSizeChanged;
        _framebufferSizeCallback = HandleFramebufferSizeChanged;
        _focusCallback = HandleFocusChanged;
        _closeCallback = HandleClose;
        _keyCallback = HandleKey;
        _mouseButtonCallback = HandleMouseButton;
        _scrollCallback = HandleScroll;
        _cursorEnterCallback = HandleCursorEnter;
        Glfw.SetWindowSizeCallback(window, _windowSizeCallback);
        Glfw.SetFramebufferSizeCallback(window, _framebufferSizeCallback);
        Glfw.SetWindowFocusCallback(window, _focusCallback);
        Glfw.SetCloseCallback(window, _closeCallback);
        Glfw.SetKeyCallback(window, _keyCallback);
        Glfw.SetMouseButtonCallback(window, _mouseButtonCallback);
        Glfw.SetScrollCallback(window, _scrollCallback);
        Glfw.SetCursorEnterCallback(window, _cursorEnterCallback);
    }

    public IntPtr WindowHandle => _window;
    public IntPtr NativeHandle => _nativeHandle;
    public int Width => _width;
    public int Height => _height;
    public float DpiScale => _dpiScale;
    public bool IsVisible => _isVisible;
    public bool IsFocused => Glfw.GetWindowAttribute(_window, WindowAttribute.Focused);
    public bool IsPointerOver => Glfw.GetWindowAttribute(_window, WindowAttribute.MouseHover);
    public bool NeedsRedraw { get; set; } = true;

    public event Action<int, int>? OnResize;
    public event Action<int, int>? OnFramebufferResize;
    public event Action<bool>? OnFocusChanged;
    public event Action? OnClose;
    public event Action<KeyboardKey, InputAction, KeyModifiers>? OnKey;
    public event Action<int, InputAction, KeyModifiers>? OnMouseButton;
    public event Action<double, double>? OnScroll;
    public event Action<bool>? OnPointerEnter;

    public void Show()
    {
        Glfw.ShowWindow(_window);
        _isVisible = true;
        NeedsRedraw = true;
    }

    public void Hide()
    {
        Glfw.HideWindow(_window);
        _isVisible = false;
    }

    public void Focus() => Glfw.FocusWindow(_window);

    public void SetPosition(int screenX, int screenY)
    {
        Glfw.SetWindowPosition(_window, screenX, screenY);
    }

    public void SetSize(int widthPoints, int heightPoints)
    {
        Glfw.SetWindowSize(_window, widthPoints, heightPoints);
    }

    public void GetPosition(out int screenX, out int screenY) =>
        Glfw.GetWindowPosition(_window, out screenX, out screenY);

    public void GetCursorPosition(out double x, out double y) =>
        Glfw.GetCursorPosition(_window, out x, out y);

    public void SetIcon(IReadOnlyList<WindowIconImage> icons)
    {
        var images = new Image[icons.Count];
        var handles = new GCHandle[icons.Count];
        try
        {
            for (var i = 0; i < icons.Count; i++)
            {
                handles[i] = GCHandle.Alloc(icons[i].Pixels, GCHandleType.Pinned);
                images[i] = new Image(icons[i].Width, icons[i].Height, handles[i].AddrOfPinnedObject());
            }
            Glfw.SetWindowIcon(_window, images.Length, images);
        }
        finally
        {
            foreach (var h in handles)
                if (h.IsAllocated) h.Free();
        }
    }

    public void RequestRedraw() => NeedsRedraw = true;

    public void MakeContextCurrent() => Glfw.MakeContextCurrent(_window);

    public string GetClipboardText() => Glfw.GetClipboardString(_window);
    public void SetClipboardText(string text) => Glfw.SetClipboardString(_window, text);

    public void RenderNow()
    {
        RenderFrame?.Invoke();
        Glfw.SwapBuffers(_window);
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

    private void HandleFocusChanged(Window window, bool focused)
    {
        OnFocusChanged?.Invoke(focused);
    }

    private void HandleClose(Window window)
    {
        OnClose?.Invoke();
    }

    private void HandleKey(Window window, Keys key, int scanCode, InputState state, ModifierKeys mods) =>
        OnKey?.Invoke(key.Adapt(), (InputAction)state, (KeyModifiers)mods);

    private void HandleMouseButton(Window window, GLFW.MouseButton button, InputState state, ModifierKeys mods) =>
        OnMouseButton?.Invoke((int)button, (InputAction)state, (KeyModifiers)mods);

    private void HandleScroll(Window window, double x, double y) => OnScroll?.Invoke(x, y);

    private void HandleCursorEnter(Window window, bool entering) => OnPointerEnter?.Invoke(entering);

    private static IntPtr ComputeNativeHandle(Window window)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return Native.GetWin32Window(window);
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return Native.GetX11Window(window);
        return window;
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
        if (_isDisposed) return;
        _isDisposed = true;
        if (!_isMain)
        {
            Glfw.DestroyWindow(_window);
            OnClosed?.Invoke();
        }
    }
}
