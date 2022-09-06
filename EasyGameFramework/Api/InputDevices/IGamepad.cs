namespace EasyGameFramework.Api.InputDevices;

public readonly struct GamepadButtonStateChangedEvent
{
    public IGamepad Gamepad { get; init; }
    public GamepadButton Button { get; init; }
}

public delegate void GamepadButtonStateChangedDelegate(GamepadButtonStateChangedEvent evt);

public interface IGamepad
{
    event GamepadButtonStateChangedDelegate ButtonPressed;
    event GamepadButtonStateChangedDelegate ButtonReleased;

    void PressButton(GamepadButton button);
    void ReleaseButton(GamepadButton button);

    bool IsButtonPressed(GamepadButton button);
}