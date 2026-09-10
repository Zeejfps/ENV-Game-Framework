using ZGF.Desktop;
using ZGF.Desktop.Input;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Tests;

/// <summary>
/// A scriptable <see cref="IWindow"/>: its size, framebuffer ratio, content scale and cursor are
/// settable, and the input events can be raised on demand — everything the scale and cursor paths
/// read from a window, with no OS behind it.
/// </summary>
internal sealed class FakeWindow : IWindow
{
    private double _cursorX, _cursorY;

    private readonly float _backingRatio;

    /// <param name="backingRatio">Framebuffer pixels per screen coordinate: 1 where the OS hands out
    /// window sizes in pixels (Windows, X11), 2 on a Retina panel.</param>
    /// <param name="contentScale">Defaults to <paramref name="backingRatio"/> — the Retina case, where
    /// the panel's extra pixels <em>are</em> the display setting. Pass the two apart for Windows,
    /// whose framebuffer stays window-sized at every scaling level.</param>
    public FakeWindow(int width = 1600, int height = 900, float backingRatio = 1f, float? contentScale = null)
    {
        _backingRatio = backingRatio;
        Width = width;
        Height = height;
        ContentScale = contentScale ?? backingRatio;
    }

    public IntPtr NativeHandle => IntPtr.Zero;
    public int Width { get; private set; }
    public int Height { get; private set; }

    public int FramebufferWidth => (int)MathF.Round(Width * _backingRatio);
    public int FramebufferHeight => (int)MathF.Round(Height * _backingRatio);

    public float DpiScale => _backingRatio;

    public float ContentScale { get; private set; }
    public bool IsVisible { get; private set; } = true;
    public bool IsFocused { get; set; } = true;
    public bool IsPointerOver { get; set; } = true;
    public bool NeedsRedraw { get; private set; } = true;

    public int PositionX { get; private set; }
    public int PositionY { get; private set; }
    public int FrameLeft { get; set; }
    public int FrameTop { get; set; }
    public int FrameRight { get; set; }
    public int FrameBottom { get; set; }
    public MouseCursor Cursor { get; private set; } = MouseCursor.Default;

    // Declared to satisfy IWindow; a test raises only the ones it drives.
#pragma warning disable CS0067
    public event Action<int, int>? OnResize;
    public event Action<int, int>? OnFramebufferResize;
    public event Action<float>? OnContentScaleChanged;
    public event Action<int, int>? OnMove;
    public event Action<bool>? OnFocusChanged;
    public event Action? OnClose;
    public event Action<KeyboardKey, InputAction, KeyModifiers>? OnKey;
    public event Action<uint>? OnText;
    public event Action<PreeditText>? OnPreedit;
    public event Action<int, InputAction, KeyModifiers>? OnMouseButton;
    public event Action<double, double>? OnScroll;
    public event Action<bool>? OnPointerEnter;
#pragma warning restore CS0067

    /// <summary>Moves the window onto a display of a different scale, the way the OS reports it on
    /// Windows: the content scale changes and nothing else does.</summary>
    public void RaiseContentScaleChanged(float contentScale)
    {
        ContentScale = contentScale;
        OnContentScaleChanged?.Invoke(contentScale);
    }

    public void SetCursorPosition(double x, double y)
    {
        _cursorX = x;
        _cursorY = y;
    }

    public void RaiseMouseButton(int button, InputAction action, KeyModifiers modifiers = KeyModifiers.None) =>
        OnMouseButton?.Invoke(button, action, modifiers);

    public void CancelClose() { }

    public void SetTextInputFocus(bool focused) { }
    public void SetPreeditCursorRect(int x, int y, int width, int height) { }
    public void ResetPreedit() { }

    public void Show() => IsVisible = true;
    public void Hide() => IsVisible = false;
    public void Focus() => IsFocused = true;

    public void SetPosition(int screenX, int screenY)
    {
        PositionX = screenX;
        PositionY = screenY;
        OnMove?.Invoke(screenX, screenY);
    }

    public void SetSize(int widthPoints, int heightPoints)
    {
        Width = widthPoints;
        Height = heightPoints;
        OnResize?.Invoke(widthPoints, heightPoints);
        OnFramebufferResize?.Invoke(FramebufferWidth, FramebufferHeight);
    }

    public void GetPosition(out int screenX, out int screenY)
    {
        screenX = PositionX;
        screenY = PositionY;
    }

    public void GetFrameSize(out int left, out int top, out int right, out int bottom)
    {
        left = FrameLeft;
        top = FrameTop;
        right = FrameRight;
        bottom = FrameBottom;
    }

    public void GetCursorPosition(out double x, out double y)
    {
        x = _cursorX;
        y = _cursorY;
    }

    public void SetCursor(MouseCursor cursor) => Cursor = cursor;
    public void SetIcon(IReadOnlyList<WindowIconImage> icons) { }
    public void RequestRedraw() => NeedsRedraw = true;

    public string GetClipboardText() => string.Empty;
    public void SetClipboardText(string text) { }

    public void Dispose() { }
}
