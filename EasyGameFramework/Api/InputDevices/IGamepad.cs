using EasyGameFramework.Api.Events;

namespace EasyGameFramework.Api.InputDevices;

public delegate void GamepadConnectedDelegate(GamepadConnectedEvent evt);
public delegate void GamepadDisconnectedDelegate(GamepadDisconnectedEvent evt);
public delegate void GamepadButtonStateChangedDelegate(GamepadButtonStateChangedEvent evt);

public interface IGamepad
{
    event GamepadButtonStateChangedDelegate ButtonPressed;
    event GamepadButtonStateChangedDelegate ButtonReleased;

    void PressButton(GamepadButton button);
    void ReleaseButton(GamepadButton button);

    bool IsButtonPressed(GamepadButton button);
}