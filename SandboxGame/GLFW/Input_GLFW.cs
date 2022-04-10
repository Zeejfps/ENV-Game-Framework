using ENV.Engine;
using ENV.Engine.InputDevices;
using GLFW;
using MouseButton = GLFW.MouseButton;

namespace ENV.GLFW.NET;

public class Input_GLFW : IInput
{
    public IMouse Mouse => m_Mouse;
    public IKeyboard Keyboard => m_Keyboard;

    private readonly Mouse_GLFW m_Mouse;
    private readonly Keyboard_GLFW m_Keyboard;
    
    public Input_GLFW()
    {
        m_Mouse = new Mouse_GLFW();
        m_Keyboard = new Keyboard_GLFW();
    }
    
    public void Update(Window window)
    {
        m_Keyboard.Update();
        m_Mouse.Update();
        
        Glfw.GetCursorPosition(window, out var x, out var y);
        m_Mouse.ScreenX = (int)x;
        m_Mouse.ScreenY = (int)y;
    }

    public void Glfw_KeyCallback(Window window, Keys glfwKey, int scancode, InputState state, ModifierKeys mods)
    {
        var keyboard = m_Keyboard;
        var key = glfwKey.ToKeyboardKey();
        switch (state)
        {
            case InputState.Release:
                keyboard.ReleaseKey(key);
                break;
            case InputState.Press:
                keyboard.PressKey(key);
                break;
            case InputState.Repeat:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }
    }
    
    public void Glfw_MousePosCallback(Window window, double x, double y)
    {
        var mouse = m_Mouse;
        mouse.ScreenX = (int)x;
        mouse.ScreenY = (int)y;
    }
    
    public void Glfw_MouseButtonCallback(Window _, MouseButton button, InputState state, ModifierKeys modifiers)
    {
        var mouseButton = MapToMouseButton(button);
        switch (state)
        {
            case InputState.Release:
                m_Mouse.ReleaseButton(mouseButton);
                break;
            case InputState.Press:
                m_Mouse.PressButton(mouseButton);
                break;
            case InputState.Repeat:
                m_Mouse.PressButton(mouseButton);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }
    }

    private Engine.InputDevices.MouseButton MapToMouseButton(MouseButton mouseButton)
    {
        switch (mouseButton)
        {
            case MouseButton.Left:
                return Engine.InputDevices.MouseButton.Left;
            case MouseButton.Right:
                return Engine.InputDevices.MouseButton.Right;
            case MouseButton.Middle:
                return Engine.InputDevices.MouseButton.Middle;
            case MouseButton.Button4:
            case MouseButton.Button5:
            case MouseButton.Button6:
            case MouseButton.Button7:
            case MouseButton.Button8:
                return new Engine.InputDevices.MouseButton((int)mouseButton);
            default:
                throw new ArgumentOutOfRangeException(nameof(mouseButton), mouseButton, null);
        }
    }
}