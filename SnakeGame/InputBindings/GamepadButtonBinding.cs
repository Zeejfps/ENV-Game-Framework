using EasyGameFramework.Api;
using EasyGameFramework.Api.InputDevices;

namespace SampleGames;

public class GamepadButtonBinding : IButtonBinding
{
    public event Action? Pressed;

    public event Action? Released;

    private readonly int m_Slot;
    private readonly GamepadButton m_Button;

    public GamepadButtonBinding(int slot, GamepadButton button)
    {
        m_Slot = slot;
        m_Button = button;
    }

    public void Bind(IInput input)
    {
        input.TryGetGamepadInSlot(m_Slot, out var gamepad);
        gamepad.ButtonPressed += OnButtonPressed;
    }

    public void Unbind()
    {
        throw new NotImplementedException();
    }

    private void OnButtonPressed(GamepadButtonStateChangedEvent evt)
    {
        
    }
}