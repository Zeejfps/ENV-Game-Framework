using System.Runtime.InteropServices;
using GLFW;
using ZGF.Desktop.Input;
using ZGF.KeyboardModule;
using ZGF.KeyboardModule.GlfwAdapter;

namespace ZGF.Desktop.Backends;

// Shared GLFW-backed IWindow plumbing: event wiring, IME, and every backend-agnostic
// property/method. Backends (OpenGL, Metal) subclass this and fill only the seams that
// differ — native handle, DPI computation, present step, and drawable-size upkeep.
public abstract class GlfwWindowBase : IWindow
{
    private readonly GlfwImeBridge _ime;
    private readonly SizeCallback _windowSizeCallback;
    private readonly SizeCallback _framebufferSizeCallback;
    private readonly PositionCallback _windowPosCallback;
    private readonly FocusCallback _focusCallback;
    private readonly WindowCallback _closeCallback;
    private readonly WindowCallback _refreshCallback;
    private readonly KeyCallback _keyCallback;
    private readonly CharCallback _charCallback;
    private readonly MouseButtonCallback _mouseButtonCallback;
    private readonly MouseCallback _scrollCallback;
    private readonly MouseEnterCallback _cursorEnterCallback;

    private int _width;
    private int _height;
    private bool _isVisible;
    private bool _isDisposed;
    private MouseCursor _currentCursor = MouseCursor.Default;

    protected float DpiScaleValue = 1f;

    public Window GlfwWindow { get; }
    public bool IsMain { get; }

    public Action? RenderFrame { get; set; }
    public event Action? OnClosed;

    protected GlfwWindowBase(Window window, bool isMain)
    {
        GlfwWindow = window;
        IsMain = isMain;
        GLFW.Glfw.GetWindowSize(window, out _width, out _height);
        _isVisible = isMain && GLFW.Glfw.GetWindowAttribute(window, WindowAttribute.Visible);

        _windowSizeCallback = HandleWindowSizeChanged;
        _framebufferSizeCallback = HandleFramebufferSizeChanged;
        _windowPosCallback = HandleWindowPositionChanged;
        _focusCallback = HandleFocusChanged;
        _closeCallback = HandleClose;
        // OS damage event (expose, restore from minimize) — rendering is gated on NeedsRedraw.
        _refreshCallback = _ => NeedsRedraw = true;
        _keyCallback = HandleKey;
        _charCallback = HandleChar;
        _mouseButtonCallback = HandleMouseButton;
        _scrollCallback = HandleScroll;
        _cursorEnterCallback = HandleCursorEnter;
        GLFW.Glfw.SetWindowSizeCallback(window, _windowSizeCallback);
        GLFW.Glfw.SetFramebufferSizeCallback(window, _framebufferSizeCallback);
        GLFW.Glfw.SetWindowPositionCallback(window, _windowPosCallback);
        GLFW.Glfw.SetWindowFocusCallback(window, _focusCallback);
        GLFW.Glfw.SetCloseCallback(window, _closeCallback);
        GLFW.Glfw.SetWindowRefreshCallback(window, _refreshCallback);
        GLFW.Glfw.SetKeyCallback(window, _keyCallback);
        GLFW.Glfw.SetCharCallback(window, _charCallback);
        GLFW.Glfw.SetMouseButtonCallback(window, _mouseButtonCallback);
        GLFW.Glfw.SetScrollCallback(window, _scrollCallback);
        GLFW.Glfw.SetCursorEnterCallback(window, _cursorEnterCallback);

        _ime = new GlfwImeBridge(window);
        _ime.OnPreedit += preedit => OnPreedit?.Invoke(preedit);
    }

    public abstract IntPtr NativeHandle { get; }

    public int Width => _width;
    public int Height => _height;
    public float DpiScale => DpiScaleValue;
    public bool IsVisible => _isVisible;
    public bool IsFocused => GLFW.Glfw.GetWindowAttribute(GlfwWindow, WindowAttribute.Focused);
    public bool IsPointerOver => GLFW.Glfw.GetWindowAttribute(GlfwWindow, WindowAttribute.MouseHover);
    public bool NeedsRedraw { get; private set; } = true;

    public event Action<int, int>? OnResize;
    public event Action<int, int>? OnFramebufferResize;
    public event Action<int, int>? OnMove;
    public event Action<bool>? OnFocusChanged;
    public event Action? OnClose;
    public event Action<KeyboardKey, InputAction, KeyModifiers>? OnKey;
    public event Action<uint>? OnText;
    public event Action<PreeditText>? OnPreedit;
    public event Action<int, InputAction, KeyModifiers>? OnMouseButton;
    public event Action<double, double>? OnScroll;
    public event Action<bool>? OnPointerEnter;

    public void SetTextInputFocus(bool focused) => _ime.SetTextInputFocus(focused);

    public void SetPreeditCursorRect(int x, int y, int width, int height) =>
        _ime.SetCursorRectangle(x, y, width, height);

    public void ResetPreedit() => _ime.Reset();

    public void Show()
    {
        GLFW.Glfw.ShowWindow(GlfwWindow);
        _isVisible = true;
        NeedsRedraw = true;
    }

    public void Hide()
    {
        GLFW.Glfw.HideWindow(GlfwWindow);
        _isVisible = false;
    }

    public void Focus() => GLFW.Glfw.FocusWindow(GlfwWindow);

    public void SetPosition(int screenX, int screenY) =>
        GLFW.Glfw.SetWindowPosition(GlfwWindow, screenX, screenY);

    public void SetSize(int widthPoints, int heightPoints) =>
        GLFW.Glfw.SetWindowSize(GlfwWindow, widthPoints, heightPoints);

    public void GetPosition(out int screenX, out int screenY) =>
        GLFW.Glfw.GetWindowPosition(GlfwWindow, out screenX, out screenY);

    public void GetFrameSize(out int left, out int top, out int right, out int bottom) =>
        GLFW.Glfw.GetWindowFrameSize(GlfwWindow, out left, out top, out right, out bottom);

    public void GetCursorPosition(out double x, out double y) =>
        GLFW.Glfw.GetCursorPosition(GlfwWindow, out x, out y);

    public void SetCursor(MouseCursor cursor)
    {
        if (cursor == _currentCursor) return;
        _currentCursor = cursor;
        GLFW.Glfw.SetCursor(GlfwWindow, GlfwStandardCursors.Get(cursor));
    }

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
            GLFW.Glfw.SetWindowIcon(GlfwWindow, images.Length, images);
        }
        finally
        {
            foreach (var h in handles)
                if (h.IsAllocated) h.Free();
        }
    }

    public void RequestRedraw() => NeedsRedraw = true;

    public string GetClipboardText() => GLFW.Glfw.GetClipboardString(GlfwWindow);
    public void SetClipboardText(string text) => GLFW.Glfw.SetClipboardString(GlfwWindow, text);

    public virtual void MakeContextCurrent() { }

    public void RenderNow()
    {
        // Cleared before drawing so a redraw requested mid-frame survives to the next iteration.
        NeedsRedraw = false;
        RenderFrame?.Invoke();
        Present();
    }

    // The backend's per-frame present step (buffer swap for OpenGL; nothing for Metal, which
    // presents its drawable inside RenderFrame).
    protected abstract void Present();

    // Current DPI scale, recomputed on framebuffer changes. Subclasses must set DpiScaleValue in
    // their constructor once their backend-specific state is ready, then this keeps it in sync.
    protected abstract float ComputeDpiScale();

    // Hooks for backends whose swapchain/layer must track the window/framebuffer size.
    protected virtual void OnWindowResized(int width, int height) { }
    protected virtual void OnFramebufferResized(int width, int height) { }

    private void HandleWindowSizeChanged(Window window, int width, int height)
    {
        _width = width;
        _height = height;
        OnWindowResized(width, height);
        OnResize?.Invoke(width, height);
    }

    private void HandleFramebufferSizeChanged(Window window, int width, int height)
    {
        OnFramebufferResized(width, height);
        DpiScaleValue = ComputeDpiScale();
        OnFramebufferResize?.Invoke(width, height);
    }

    private void HandleWindowPositionChanged(Window window, int x, int y) => OnMove?.Invoke(x, y);

    private void HandleFocusChanged(Window window, bool focused) => OnFocusChanged?.Invoke(focused);

    private void HandleClose(Window window) => OnClose?.Invoke();

    private void HandleKey(Window window, Keys key, int scanCode, InputState state, ModifierKeys mods) =>
        OnKey?.Invoke(key.Adapt(), (InputAction)state, (KeyModifiers)mods);

    // GLFW hands us the code point the OS committed, with layout, modifiers and dead keys already
    // applied, and it filters control codes — so this fires for 'й' and 'é' but not for Ctrl/Cmd+C.
    private void HandleChar(Window window, uint codePoint) => OnText?.Invoke(codePoint);

    private void HandleMouseButton(Window window, MouseButton button, InputState state, ModifierKeys mods) =>
        OnMouseButton?.Invoke((int)button, (InputAction)state, (KeyModifiers)mods);

    private void HandleScroll(Window window, double x, double y) => OnScroll?.Invoke(x, y);

    private void HandleCursorEnter(Window window, bool entering) => OnPointerEnter?.Invoke(entering);

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        if (!IsMain)
        {
            GLFW.Glfw.DestroyWindow(GlfwWindow);
            OnClosed?.Invoke();
        }
    }
}
