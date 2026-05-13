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
    private readonly OpenGlRenderedCanvas _canvas;

    // Held so the delegates aren't GC'd while GLFW holds native pointers to them.
    private readonly KeyCallback _keyCallback;
    private readonly MouseButtonCallback _mouseButtonCallback;
    private readonly MouseCallback _scrollCallback;

    public InputSystem InputSystem { get; } = new();
    public Mouse Mouse { get; } = new();

    public GlfwInputSystem(GlfwWindow windowHandle, OpenGlRenderedCanvas canvas)
    {
        _windowHandle = windowHandle;
        _canvas = canvas;

        _keyCallback = HandleKeyEvent;
        _mouseButtonCallback = HandleMouseButtonEvent;
        _scrollCallback = HandleScrollEvent;
        Glfw.SetKeyCallback(_windowHandle, _keyCallback);
        Glfw.SetMouseButtonCallback(_windowHandle, _mouseButtonCallback);
        Glfw.SetScrollCallback(_windowHandle, _scrollCallback);
    }

    public void Update()
    {
        Glfw.GetCursorPosition(_windowHandle, out var mouseX, out var mouseY);
        var guiPoint = WindowToGuiCoords(mouseX, mouseY);
        var prevPoint = Mouse.Point;
        Mouse.Point = guiPoint;
        if (prevPoint == guiPoint)
            return;

        var e = new MouseMoveEvent
        {
            Mouse = Mouse,
            Phase = EventPhase.Capturing,
        };
        InputSystem.SendMouseMovedEvent(ref e);
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
    }

    private void HandleMouseButtonEvent(GlfwWindow window, GLFW.MouseButton button, GLFW.InputState state, ModifierKeys modifiers)
    {
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
            Phase = EventPhase.Capturing,
        };
        InputSystem.SendMouseButtonEvent(ref e);
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
