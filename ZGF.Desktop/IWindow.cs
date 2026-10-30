using ZGF.Desktop.Input;
using ZGF.Geometry;
using ZGF.KeyboardModule;

namespace ZGF.Desktop;

public interface IWindow : IDisposable
{
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
    bool IsMaximized { get; }
    bool IsMinimized { get; }
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
    // Minimized to the dock or taskbar (true) or restored from it (false).
    event Action<bool> OnMinimizedChanged;
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
    // Files or folders dragged from the OS file manager and dropped on the client area, as full
    // paths in the order the platform lists them. The platform moves the cursor to the drop point first.
    event Action<IReadOnlyList<string>> OnFileDrop;
    // A press on the native frame (title bar, borders, caption buttons), which GLFW never reports as
    // a mouse event. Raised only where the platform has a frame hook (Windows).
    event Action OnNonClientPress;

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
    void Minimize();
    void Maximize();
    // Back from minimized or maximized to the window's normal size and place.
    void Restore();
    void SetPosition(int screenX, int screenY);
    void SetSize(int widthPoints, int heightPoints);
    void GetPosition(out int screenX, out int screenY);
    // Thickness of the native decoration (title bar, borders) around the client area, in screen
    // coordinates. All zeros for borderless windows (popups, fullscreen).
    void GetFrameSize(out int left, out int top, out int right, out int bottom);
    void GetCursorPosition(out double x, out double y);
    void SetCursor(MouseCursor cursor);
    // Hides the cursor and keeps it to the window while locked, its position then moving without bound, so
    // how far it moves is read rather than where it is: for steering by the mouse. Raw motion where the
    // platform has it.
    void SetPointerLocked(bool locked);
    // Lets every pointer event fall through to whatever is behind the window — for a tooltip.
    void SetMousePassThrough(bool passThrough);
    // Routes every press to this window until released, so a press outside it reaches
    // onOutsideClick before it goes on to whatever is under the cursor. Windows only.
    void CapturePointer(Action<PointI> onOutsideClick);
    void ReleasePointerCapture();
    // Native modality: whether the OS lets the user interact with this window at all, and which
    // window it stays in front of. Windows only.
    void SetInputEnabled(bool enabled);
    void SetOwner(IWindow owner);
    void SetIcon(IReadOnlyList<WindowIconImage> icons);
    // Matches the native title bar to a dark or light client area where the platform draws one
    // (Windows' immersive dark caption); a no-op elsewhere.
    void SetTitleBarTheme(bool dark);
    // Whether the content can reach under the native title bar while the platform keeps drawing its
    // caption buttons there. Windows 11 only.
    bool CanExtendIntoTitleBar { get; }
    // Lets the content reach under the native title bar, drawn as content describes, or with null gives
    // the title bar its own strip again. A shown window keeps its place on the screen, so its content
    // grows or shrinks; a hidden one keeps its content's size. A no-op where CanExtendIntoTitleBar is false.
    void ExtendIntoTitleBar(TitleBarContent? content);
    // Where the platform draws its caption buttons over the content, in window coordinates: the room
    // the content leaves free. Empty while the title bar has its own strip.
    RectI CaptionButtons { get; }

    // How many pixels of the content, from its top, lie off the screen: where a maximized window's
    // frame reaches past its monitor and the content starts at the frame's top (Windows).
    int HiddenContentTop { get; }
    void RequestRedraw();

    string GetClipboardText();
    void SetClipboardText(string text);
}
