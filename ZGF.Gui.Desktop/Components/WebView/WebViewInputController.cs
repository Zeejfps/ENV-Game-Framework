using ZGF.Desktop;
using ZGF.Geometry;
using ZGF.Gui.Desktop.Controllers;
using ZGF.Gui.Desktop.Input;
using ZGF.Gui.Views;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Desktop.Components.WebView;

/// <summary>
/// Forwards ZGF pointer / keyboard / focus input into an <see cref="IWebView"/> and reflects the
/// cursor the page requests. Coordinates are converted from the canvas's Y-up logical space into the
/// widget-local, Y-down points CEF expects. Wheel over the view scrolls the page and is consumed so it
/// doesn't bubble to an enclosing scroll pane (plan decision 10).
/// </summary>
internal sealed class WebViewInputController : KeyboardMouseController, IProvidesCursor, IDisposable
{
    private readonly WebViewView _view;
    private readonly IWebView _webView;
    private readonly InputSystem _input;
    private MouseCursor _cursor = MouseCursor.Default;

    public WebViewInputController(WebViewView view, IWebView webView, InputSystem input)
    {
        _view = view;
        _webView = webView;
        _input = input;
        _webView.CursorChanged += OnCursorChanged;
    }

    public MouseCursor Cursor => _cursor;

    public void Dispose() => _webView.CursorChanged -= OnCursorChanged;

    private void OnCursorChanged(WebCursor cursor)
    {
        // Stored only; the input system polls DesiredCursor each tick and applies it. CursorChanged
        // fires during a CEF pump, which already runs inside a tick, so it takes effect promptly.
        _cursor = Map(cursor);
    }

    // Canvas logical space is Y-up (Top = visual top); CEF wants widget-local points with (0,0) at the
    // top-left and Y increasing downward.
    private (float x, float y) Local(PointF p) =>
        (p.X - _view.Position.Left, _view.Position.Top - p.Y);

    public override void OnMouseMoved(ref MouseMoveEvent e)
    {
        var (x, y) = Local(e.Mouse.Point);
        _webView.SendMouseMove(x, y, Mods(e.Mouse));
    }

    public override void OnMouseExit(ref MouseExitEvent e) => _webView.SendMouseLeave();

    public override void OnMouseButtonStateChanged(ref MouseButtonEvent e)
    {
        var (x, y) = Local(e.Mouse.Point);
        var pressed = e.State == InputState.Pressed;
        // Grab keyboard focus on press so typing routes into the page.
        if (pressed)
            _input.StealFocus(this);
        _webView.SendMouseButton(x, y, MapButton(e.Button), pressed, 1, Mods(e.Mouse) | Modifiers(e.Modifiers));
    }

    public override void OnMouseWheelScrolled(ref MouseWheelScrolledEvent e)
    {
        var (x, y) = Local(e.Mouse.Point);
        // CEF wheel deltas are in pixels; ZGF deltas are in notches — scale to a sensible step.
        const float step = 40f;
        _webView.SendMouseWheel(x, y, e.DeltaX * step, e.DeltaY * step, Mods(e.Mouse));
        e.Consume();
    }

    public override void OnKeyboardKeyStateChanged(ref KeyboardKeyEvent e)
    {
        // Text-producing keys are delivered as characters via OnTextInput (which synthesizes the full
        // RawKeyDown→Char→KeyUp sequence CEF needs). Forward here only the non-text keys — navigation,
        // editing, modifiers — plus any key pressed with a command modifier (Ctrl/Alt/Super shortcuts,
        // which produce no text). This avoids double-delivering ordinary typed characters.
        var command = e.Modifiers.HasFlag(InputModifiers.Control)
                      || e.Modifiers.HasFlag(InputModifiers.Alt)
                      || e.Modifiers.HasFlag(InputModifiers.Super);
        if (!IsForwardedKey(e.Key) && !command)
            return;

        var pressed = e.State == InputState.Pressed;
        var vk = VirtualKey(e.Key);
        _webView.SendKey(pressed ? WebKeyEventKind.RawKeyDown : WebKeyEventKind.KeyUp,
            vk, vk, '\0', Modifiers(e.Modifiers), isSystemKey: false);
        // Claim the keystroke so navigation/edit keys reach the page rather than bubbling to app shortcuts.
        if (pressed)
            e.ConsumeAsText();
    }

    public override void OnTextInput(ref TextInputEvent e)
    {
        // CEF/Blink inserts a character from a RawKeyDown → Char → KeyUp sequence, not a lone Char, and
        // wants a non-zero key code on the events — synthesize the sequence per UTF-16 unit.
        Span<char> chars = stackalloc char[2];
        var n = e.Rune.EncodeToUtf16(chars);
        for (var i = 0; i < n; i++)
        {
            var c = chars[i];
            int vk = char.ToUpperInvariant(c);
            _webView.SendKey(WebKeyEventKind.RawKeyDown, vk, vk, '\0', WebInputModifiers.None, isSystemKey: false);
            _webView.SendKey(WebKeyEventKind.Char, vk, vk, c, WebInputModifiers.None, isSystemKey: false);
            _webView.SendKey(WebKeyEventKind.KeyUp, vk, vk, '\0', WebInputModifiers.None, isSystemKey: false);
        }
        e.Consume();
    }

    public override void OnFocusGained() => _webView.SetFocus(true);
    public override void OnFocusLost() => _webView.SetFocus(false);

    private WebInputModifiers Mods(IMouse mouse)
    {
        var m = WebInputModifiers.None;
        if (mouse.IsButtonPressed(MouseButton.Left)) m |= WebInputModifiers.LeftMouseButton;
        if (mouse.IsButtonPressed(MouseButton.Middle)) m |= WebInputModifiers.MiddleMouseButton;
        if (mouse.IsButtonPressed(MouseButton.Right)) m |= WebInputModifiers.RightMouseButton;
        return m;
    }

    private static WebInputModifiers Modifiers(InputModifiers mods)
    {
        var m = WebInputModifiers.None;
        if (mods.HasFlag(InputModifiers.Shift)) m |= WebInputModifiers.Shift;
        if (mods.HasFlag(InputModifiers.Control)) m |= WebInputModifiers.Control;
        if (mods.HasFlag(InputModifiers.Alt)) m |= WebInputModifiers.Alt;
        if (mods.HasFlag(InputModifiers.Super)) m |= WebInputModifiers.Meta;
        return m;
    }

    private static WebMouseButton MapButton(MouseButton b)
    {
        // Glfw's Left/Right/Middle are aliases of Button1/2/3, so they aren't usable as constant
        // switch patterns — compare with equality instead.
        if (b == MouseButton.Right) return WebMouseButton.Right;
        if (b == MouseButton.Middle) return WebMouseButton.Middle;
        return WebMouseButton.Left;
    }

    private static MouseCursor Map(WebCursor c) => c switch
    {
        WebCursor.Hand or WebCursor.Pointer => MouseCursor.Hand,
        WebCursor.Text => MouseCursor.Text,
        WebCursor.Crosshair => MouseCursor.Crosshair,
        WebCursor.ResizeHorizontal => MouseCursor.ResizeHorizontal,
        WebCursor.ResizeVertical => MouseCursor.ResizeVertical,
        _ => MouseCursor.Default,
    };

    // Keys forwarded as raw key events (they produce no text of their own): navigation, editing,
    // whitespace/return, function and modifier keys. Everything else is delivered via OnTextInput.
    private static bool IsForwardedKey(KeyboardKey key) => key switch
    {
        KeyboardKey.Backspace or KeyboardKey.Tab or KeyboardKey.Enter or KeyboardKey.NumpadEnter
            or KeyboardKey.Escape or KeyboardKey.Delete or KeyboardKey.Insert
            or KeyboardKey.Home or KeyboardKey.End or KeyboardKey.PageUp or KeyboardKey.PageDown
            or KeyboardKey.LeftArrow or KeyboardKey.RightArrow or KeyboardKey.UpArrow or KeyboardKey.DownArrow
            or KeyboardKey.LeftShift or KeyboardKey.RightShift or KeyboardKey.LeftControl or KeyboardKey.RightControl
            or KeyboardKey.LeftAlt or KeyboardKey.RightAlt or KeyboardKey.LeftSuper or KeyboardKey.RightSuper => true,
        >= KeyboardKey.F1 and <= KeyboardKey.F25 => true,
        _ => false,
    };

    // Common ZGF keys → Windows virtual-key codes (what CEF's WindowsKeyCode expects). Letters/digits
    // map to their ASCII code; the rest cover navigation/editing. Unmapped keys pass 0.
    private static int VirtualKey(KeyboardKey key)
    {
        if (key is >= KeyboardKey.A and <= KeyboardKey.Z)
            return 'A' + (key - KeyboardKey.A);
        if (key is >= KeyboardKey.Alpha0 and <= KeyboardKey.Alpha9)
            return '0' + (key - KeyboardKey.Alpha0);
        if (key is >= KeyboardKey.F1 and <= KeyboardKey.F12)
            return 0x70 + (key - KeyboardKey.F1);
        return key switch
        {
            KeyboardKey.Backspace => 0x08,
            KeyboardKey.Tab => 0x09,
            KeyboardKey.Enter or KeyboardKey.NumpadEnter => 0x0D,
            KeyboardKey.Escape => 0x1B,
            KeyboardKey.Space => 0x20,
            KeyboardKey.PageUp => 0x21,
            KeyboardKey.PageDown => 0x22,
            KeyboardKey.End => 0x23,
            KeyboardKey.Home => 0x24,
            KeyboardKey.LeftArrow => 0x25,
            KeyboardKey.UpArrow => 0x26,
            KeyboardKey.RightArrow => 0x27,
            KeyboardKey.DownArrow => 0x28,
            KeyboardKey.Insert => 0x2D,
            KeyboardKey.Delete => 0x2E,
            KeyboardKey.LeftShift or KeyboardKey.RightShift => 0x10,
            KeyboardKey.LeftControl or KeyboardKey.RightControl => 0x11,
            KeyboardKey.LeftAlt or KeyboardKey.RightAlt => 0x12,
            _ => 0,
        };
    }
}
