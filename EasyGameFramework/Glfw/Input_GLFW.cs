using EasyGameFramework.Api;
using EasyGameFramework.Api.InputDevices;

namespace EasyGameFramework.Glfw;

public class Input_GLFW : IInput
{
    private readonly Keyboard_GLFW m_Keyboard;

    private readonly Mouse_GLFW m_Mouse;

    public Input_GLFW()
    {
        m_Mouse = new Mouse_GLFW();
        m_Keyboard = new Keyboard_GLFW();
    }

    public IMouse Mouse => m_Mouse;
    public IKeyboard Keyboard => m_Keyboard;

    public void Update()
    {
        m_Keyboard.Update();
        m_Mouse.Update();
    }
}