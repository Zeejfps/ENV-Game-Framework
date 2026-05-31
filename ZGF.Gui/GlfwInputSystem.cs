using GLFW;
using ZGF.Geometry;
using ZGF.Gui.Desktop;
using ZGF.KeyboardModule.GlfwAdapter;
using GlfwWindow = GLFW.Window;
using InputState = ZGF.Gui.Desktop.InputState;
using MouseButton = ZGF.Gui.Desktop.MouseButton;

namespace ZGF.Gui;

public sealed class GlfwInputSystem
{
    private readonly GlfwWindow _windowHandle;
    private readonly RenderedCanvasBase _canvas;

    // Held so the delegates aren't GC'd while GLFW holds native pointers to them.
    private readonly KeyCallback _keyCallback;
    private readonly MouseButtonCallback _mouseButtonCallback;
    private readonly MouseCallback _scrollCallback;
    private readonly MouseEnterCallback _cursorEnterCallback;
    private bool _pendingExitClear;
    private string? _lastDiagReason;

    public InputSystem InputSystem { get; } = new();
    public Mouse Mouse { get; } = new();
    public Action? OnAnyInput { get; set; }

    public GlfwInputSystem(IntPtr windowHandle, RenderedCanvasBase canvas)
        : this((GlfwWindow)windowHandle, canvas) { }

    public GlfwInputSystem(GlfwWindow windowHandle, RenderedCanvasBase canvas)
    {
        _windowHandle = windowHandle;
        _canvas = canvas;

        _keyCallback = HandleKeyEvent;
        _mouseButtonCallback = HandleMouseButtonEvent;
        _scrollCallback = HandleScrollEvent;
        _cursorEnterCallback = HandleCursorEnter;
        Glfw.SetKeyCallback(_windowHandle, _keyCallback);
        Glfw.SetMouseButtonCallback(_windowHandle, _mouseButtonCallback);
        Glfw.SetScrollCallback(_windowHandle, _scrollCallback);
        Glfw.SetCursorEnterCallback(_windowHandle, _cursorEnterCallback);
    }

    /// <summary>
    /// Clear all transient input state so this instance can be reused for a fresh
    /// popup. Resets the underlying <see cref="InputSystem"/> (focus/hover/path) and
    /// the locally latched fields (pending exit-clear, last-known cursor point). Call
    /// when the owning popup window is returned to the pool — otherwise a leftover
    /// focused component keeps <see cref="Desktop.InputSystem.HasFocus"/> true and
    /// <see cref="Update"/> short-circuits before hover/click dispatch, leaving every
    /// subsequently pooled popup dead until the app restarts.
    /// </summary>
    public void Reset()
    {
        InputSystem.Reset();
        _pendingExitClear = false;
        _lastDiagReason = null;
        Mouse.Point = new PointF(float.MinValue, float.MinValue);
    }

    private void HandleCursorEnter(GlfwWindow window, bool entering)
    {
        // GLFW only fires this on enter/exit transitions. When the cursor leaves
        // the window we can't safely send a synthetic MouseMove right now (we may
        // be inside GLFW's event-pump and the view tree isn't reentrant); defer
        // the hover-clear to the next Update() tick.
        if (!entering) _pendingExitClear = true;
    }

    // Popup-only: log when Update's effective state changes between ticks (focus-latched
    // short-circuit / cursor-not-hovering / actively dispatching hover). A popup stuck in
    // "focus-latch" or "no-mousehover" is one that shows but builds no dispatch path — the
    // root of the dead-context-menu bug.
    private void DiagReason(string reason)
    {
        if (InputSystem.DiagLabel == null) return;
        if (_lastDiagReason == reason) return;
        _lastDiagReason = reason;
        Console.WriteLine($"[ctxmenu-diag {InputSystem.DiagLabel}] update-state={reason}");
    }

    public void Update()
    {
        if (InputSystem.HasFocus)
        {
            DiagReason("focus-latch (Update short-circuits before hover; clicks route to focused/empty queue)");
            Glfw.GetCursorPosition(_windowHandle, out var capturedX, out var capturedY);
            var capturedPoint = WindowToGuiCoords(capturedX, capturedY);
            if (capturedPoint != Mouse.Point)
            {
                Mouse.Point = capturedPoint;
                var capturedEvent = new MouseMoveEvent
                {
                    Mouse = Mouse,
                    Phase = EventPhase.Capturing,
                };
                InputSystem.SendMouseMovedEvent(ref capturedEvent);
                OnAnyInput?.Invoke();
            }
            return;
        }
        
        if (_pendingExitClear)
        {
            _pendingExitClear = false;
            var prev = Mouse.Point;
            Mouse.Point = new PointF(float.MinValue, float.MinValue);
            var exitEvent = new MouseMoveEvent
            {
                Mouse = Mouse,
                Phase = EventPhase.Capturing,
            };
            InputSystem.SendMouseMovedEvent(ref exitEvent);
            // Don't restore Mouse.Point — the cursor is genuinely outside; the
            // next Update with MouseHover=true will refresh from GLFW.
            _ = prev;
        }

        // Freeze hover state when the cursor leaves this window's bounds. Without
        // this, Glfw.GetCursorPosition reports stale coords outside the window's
        // rect and the hover path would clear — defeating, e.g., tooltip hover
        // while the cursor sits over the tooltip's own popup window.
        //
        // Test the cursor against the window rect directly rather than via GLFW's
        // MouseHover (GLFW_HOVERED) attribute. That attribute is updated only from OS
        // cursor enter/leave events, which fire solely on cursor *movement*: a context
        // menu that pops up directly under a stationary cursor never receives an enter
        // event, so GLFW_HOVERED stays false, Update short-circuits here, and the menu
        // opens dead until the mouse is jiggled. A direct bounds check reports hover on
        // the first tick while still freezing once the cursor genuinely leaves the rect.
        Glfw.GetCursorPosition(_windowHandle, out var mouseX, out var mouseY);
        Glfw.GetWindowSize(_windowHandle, out var winWidth, out var winHeight);
        var cursorInsideBounds =
            mouseX >= 0 && mouseY >= 0 && mouseX <= winWidth && mouseY <= winHeight;
        if (!cursorInsideBounds)
        {
            DiagReason("cursor-outside-bounds (cursor not within window rect; hover/queue not built)");
            return;
        }
        DiagReason("active (hover dispatching normally)");
        var guiPoint = WindowToGuiCoords(mouseX, mouseY);
        var prevPoint = Mouse.Point;
        Mouse.Point = guiPoint;
        if (prevPoint == guiPoint)
        {
            InputSystem.RefreshHover(Mouse);
            return;
        }

        var e = new MouseMoveEvent
        {
            Mouse = Mouse,
            Phase = EventPhase.Capturing,
        };
        InputSystem.SendMouseMovedEvent(ref e);
        OnAnyInput?.Invoke();
    }

    private void HandleScrollEvent(GlfwWindow window, double x, double y)
    {
        var e = new MouseWheelScrolledEvent
        {
            Mouse = Mouse,
            DeltaX = (float)x,
            DeltaY = (float)y,
            Phase = EventPhase.Capturing
        };
        InputSystem.SendMouseScrollEvent(ref e);
        OnAnyInput?.Invoke();
    }

    private void HandleMouseButtonEvent(GlfwWindow window, GLFW.MouseButton button, GLFW.InputState state, ModifierKeys modifiers)
    {
        Glfw.GetCursorPosition(_windowHandle, out var mouseX, out var mouseY);
        Mouse.Point = WindowToGuiCoords(mouseX, mouseY);
        var b = button switch
        {
            GLFW.MouseButton.Left => MouseButton.Left,
            GLFW.MouseButton.Right => MouseButton.Right,
            GLFW.MouseButton.Middle => MouseButton.Middle,
            _ => new MouseButton((int)button),
        };
        var s = state switch
        {
            GLFW.InputState.Press => InputState.Pressed,
            GLFW.InputState.Release => InputState.Released,
            GLFW.InputState.Repeat => InputState.Pressed,
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
        };

        if (s == InputState.Pressed)
        {
            Mouse.Press(b);
        }
        else
        {
            Mouse.Release(b);
        }

        var e = new MouseButtonEvent
        {
            Mouse = Mouse,
            Button = b,
            State = s,
            Modifiers = (InputModifiers)modifiers,
            Phase = EventPhase.Capturing,
        };
        InputSystem.SendMouseButtonEvent(ref e);
        OnAnyInput?.Invoke();
    }

    private void HandleKeyEvent(GlfwWindow window, Keys key, int scanCode, GLFW.InputState state, ModifierKeys mods)
    {
        var s = state switch
        {
            GLFW.InputState.Press => InputState.Pressed,
            GLFW.InputState.Release => InputState.Released,
            GLFW.InputState.Repeat => InputState.Pressed,
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
        };

        var e = new KeyboardKeyEvent
        {
            Key = key.Adapt(),
            State = s,
            Modifiers = (InputModifiers)mods,
            Phase = EventPhase.Capturing
        };
        InputSystem.SendKeyboardKeyEvent(ref e);
        OnAnyInput?.Invoke();
    }

    private PointF WindowToGuiCoords(double windowX, double windowY)
    {
        Glfw.GetWindowSize(_windowHandle, out var width, out var height);
        var scaleX = _canvas.Width / (float)width;
        var scaleY = _canvas.Height / (float)height;
        var screenX = windowX * scaleX;
        var screenY = (height - windowY) * scaleY;
        return new PointF((float)screenX, (float)screenY);
    }
}
