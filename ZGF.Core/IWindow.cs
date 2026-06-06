using ZGF.KeyboardModule;

namespace ZGF.Core;

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
    event Action<bool> OnFocusChanged;
    event Action OnClose;

    event Action<KeyboardKey, InputAction, KeyModifiers> OnKey;
    event Action<int, InputAction, KeyModifiers> OnMouseButton;
    event Action<double, double> OnScroll;
    event Action<bool> OnPointerEnter;

    void Show();
    void Hide();
    void Focus();
    void SetPosition(int screenX, int screenY);
    void SetSize(int widthPoints, int heightPoints);
    void GetPosition(out int screenX, out int screenY);
    void GetCursorPosition(out double x, out double y);
    void SetIcon(IReadOnlyList<WindowIconImage> icons);
    void RequestRedraw();
    void RenderNow();
    void MakeContextCurrent();
    
    string GetClipboardText();
    void SetClipboardText(string text);
}
