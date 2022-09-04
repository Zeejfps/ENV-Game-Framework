using EasyGameFramework.Api;
using EasyGameFramework.Api.InputDevices;

namespace EasyGameFramework.Core;

internal class Input : IInput
{
    private readonly Keyboard m_Keyboard;

    private readonly Mouse m_Mouse;

    public Input(IEventBus eventBus)
    {
        m_Mouse = new Mouse();
        m_Keyboard = new Keyboard(eventBus);
    }

    public IMouse Mouse => m_Mouse;
    public IKeyboard Keyboard => m_Keyboard;

    public void Update()
    {
        m_Keyboard.Reset();
        m_Mouse.Update();
    }
}