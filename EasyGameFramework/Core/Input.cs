using EasyGameFramework.Api;
using EasyGameFramework.Api.InputDevices;

namespace EasyGameFramework.Glfw;

internal class Input : IInput
{
    private readonly Keyboard m_Keyboard;

    private readonly Mouse m_Mouse;

    public Input()
    {
        m_Mouse = new Mouse();
        m_Keyboard = new Keyboard();
    }

    public IMouse Mouse => m_Mouse;
    public IKeyboard Keyboard => m_Keyboard;

    public void Update()
    {
        m_Keyboard.Update();
        m_Mouse.Update();
    }
}