using EasyGameFramework.API;
using EasyGameFramework.API.InputDevices;

namespace Framework.GLFW.NET;

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
    
    public void Update()
    {
        m_Keyboard.Update();
        m_Mouse.Update();
    }
}