using ZGF.Desktop.Input;
using ZGF.KeyboardModule;

namespace ZGF.Desktop;

public interface IWindow : IDisposable
{
    // The OS-native window handle (Win32 HWND / Cocoa NSWindow / X11 Window). Native chrome
    // and popup decorators consume this so they never touch the windowing backend.
    IntPtr NativeHandle { get; }
    int Width { get; }
    int Height { get; }
    float DpiScale { get; }
    bool IsVisible { get; }
    bool IsFocused { get; }
    bool IsPointerOver { get; }
    bool NeedsRedraw { get; }

    event Action<int, int> OnResize;
    event Action<int, int> OnFramebufferResize;
    // Window moved: new top-left position in screen coordinates.
    event Action<int, int> OnMove;
    event Action<bool> OnFocusChanged;
    event Action OnClose;

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

    // Whether the OS IME may compose on this window. Off outside a text field, or a CJK IME would
    // start composing on the keys that drive list navigation. No-op without a patched GLFW.
    void SetImeEnabled(bool enabled);
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
    void RenderNow();
    void MakeContextCurrent();
    
    string GetClipboardText();
    void SetClipboardText(string text);
}
