using EasyGameFramework.Api.InputDevices;

namespace SimplePlatformer;

public sealed class GamepadButtonToButtonInputBinding : IButtonInputBinding
{
    private readonly GamepadButton m_Button;

    public GamepadButtonToButtonInputBinding(GamepadButton button)
    {
        m_Button = button;
    }

    public bool Poll(IKeyboard keyboard, IMouse mouse, IGamepad? gamepad)
    {
        if (gamepad == null)
            return false;

        return gamepad.IsButtonPressed(m_Button);
    }
}