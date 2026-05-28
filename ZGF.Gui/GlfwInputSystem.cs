using GLFW;
using ZGF.Geometry;
using ZGF.KeyboardModule.GlfwAdapter;
using GlfwWindow = GLFW.Window;
using InputState = ZGF.Gui.InputState;
using MouseButton = ZGF.Gui.MouseButton;

namespace ZGF.Gui.Tests;

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

    private void HandleCursorEnter(GlfwWindow window, bool entering)
    {
        // GLFW only fires this on enter/exit transitions. When the cursor leaves
        // the window we can't safely send a synthetic MouseMove right now (we may
        // be inside GLFW's event-pump and the view tree isn't reentrant); defer
        // the hover-clear to the next Update() tick.
        if (!entering) _pendingExitClear = true;
    }

    public void Update()
    { 
        if (InputSystem.HasFocus)
        {
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
        if (!Glfw.GetWindowAttribute(_windowHandle, WindowAttribute.MouseHover)) return;
        Glfw.GetCursorPosition(_windowHandle, out var mouseX, out var mouseY);
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
