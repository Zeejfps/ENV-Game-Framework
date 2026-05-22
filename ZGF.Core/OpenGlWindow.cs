using GLFW;

namespace ZGF.Core;

public sealed class OpenGlWindow : IWindow
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
    public bool IsMain => _isMain;

    public Action? RenderFrame { get; set; }
    public event Action? OnClosed;

    public OpenGlWindow(Window window, bool isMain)
    {
        _window = window;
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
    public event Action? OnFocusChanged;
    public event Action? OnClose;

    public void Show()
    {
        Glfw.ShowWindow(_window);
        _isVisible = true;
    }

    public void Hide()
    {
        Glfw.HideWindow(_window);
        _isVisible = false;
    }

    public void SetPosition(int screenX, int screenY)
    {
        Glfw.SetWindowPosition(_window, screenX, screenY);
    }

    public void SetSize(int widthPoints, int heightPoints)
    {
        Glfw.SetWindowSize(_window, widthPoints, heightPoints);
    }

    public void RequestRedraw() => NeedsRedraw = true;

    public void MakeContextCurrent() => Glfw.MakeContextCurrent(_window);

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
        OnFocusChanged?.Invoke();
    }

    private void HandleClose(Window window)
    {
        OnClose?.Invoke();
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
