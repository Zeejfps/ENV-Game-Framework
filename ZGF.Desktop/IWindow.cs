using ZGF.Desktop.Input;
using ZGF.KeyboardModule;

namespace ZGF.Desktop;

public interface IWindow : IDisposable
{
    // The OS-native window handle (Win32 HWND / Cocoa NSWindow / X11 Window). Native chrome
    // and popup decorators consume this so they never touch the windowing backend.
    IntPtr NativeHandle { get; }
    // The client area in screen coordinates. Not pixels: on a Retina panel a window is half as many
    // points wide as it is pixels, which is what FramebufferWidth reports.
    int Width { get; }
    int Height { get; }

    // The drawable surface in pixels. The numerator of every logical size: it is the one number that
    // means the same thing on a platform that scales by enlarging points (macOS) and one that scales
    // by putting more pixels in a point (Windows).
    int FramebufferWidth { get; }
    int FramebufferHeight { get; }

    /// Framebuffer pixels per screen coordinate. 1 wherever the OS hands out window sizes in pixels
    /// (Windows, X11), 2 on a Retina panel.
    float DpiScale { get; }

    /// The display scaling the OS asks applications to honour on the monitor this window is on:
    /// device pixels per logical point, 1.5 at Windows' 150%. Distinct from <see cref="DpiScale"/>,
    /// which is a property of the framebuffer rather than of the display setting — on Windows the
    /// framebuffer follows the window's pixel size at every scaling level, so the two disagree.
    float ContentScale { get; }
    bool IsVisible { get; }
    bool IsFocused { get; }
    bool IsPointerOver { get; }
    bool NeedsRedraw { get; }

    event Action<int, int> OnResize;
    event Action<int, int> OnFramebufferResize;
    // The monitor's content scale changed under this window — dragged to a display with different
    // scaling, or the display setting changed. On Windows this is the only event that fires: the
    // window keeps its pixel size, so neither a resize nor a framebuffer resize is reported.
    event Action<float> OnContentScaleChanged;
    // Window moved: new top-left position in screen coordinates.
    event Action<int, int> OnMove;
    event Action<bool> OnFocusChanged;
    event Action OnClose;
    // Withdraws a close request while handling OnClose, keeping the window — and, for the main
    // window, the run loop — alive. The platform marks a window as closing before it raises the
    // request, so a handler that wants to ask the user something first has to unmark it here.
    void CancelClose();

    event Action<KeyboardKey, InputAction, KeyModifiers> OnKey;
    // A character committed by the OS text-input pipeline, as a Unicode code point — already
    // resolved for keyboard layout, modifiers and dead keys. OnKey carries physical key positions
    // and cannot be decoded into text without hard-coding a US layout; this is the text path.
    event Action<uint> OnText;
    // The in-flight IME composition, replaced wholesale on every update and empty when the
    // composition ends. Purely additive to OnText: committed text still arrives there, so a
    // keyboard that never composes (Latin, Cyrillic) never raises this.
    event Action<PreeditText> OnPreedit;
    event Action<int, InputAction, KeyModifiers> OnMouseButton;
    event Action<double, double> OnScroll;
    event Action<bool> OnPointerEnter;

    // Whether this window is editing text. Off outside a text field, or a CJK IME swallows the keys
    // that drive list navigation before the app sees them. No-op without a patched GLFW.
    void SetTextInputFocus(bool focused);
    // Where the OS candidate window should sit, in window coordinates with a top-left origin.
    void SetPreeditCursorRect(int x, int y, int width, int height);
    // Discards any in-flight composition rather than committing it.
    void ResetPreedit();

    void Show();
    void Hide();
    void Focus();
    void SetPosition(int screenX, int screenY);
    void SetSize(int widthPoints, int heightPoints);
    void GetPosition(out int screenX, out int screenY);
    // Thickness of the native decoration (title bar, borders) around the client area, in screen
    // coordinates. All zeros for borderless windows (popups, fullscreen).
    void GetFrameSize(out int left, out int top, out int right, out int bottom);
    void GetCursorPosition(out double x, out double y);
    void SetCursor(MouseCursor cursor);
    void SetIcon(IReadOnlyList<WindowIconImage> icons);
    void RequestRedraw();

    string GetClipboardText();
    void SetClipboardText(string text);
}
