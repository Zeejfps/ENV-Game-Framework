namespace EasyGameFramework.Api.InputDevices;

public readonly struct GamepadButtonStateChangedEvent
{
    public IGenericGamepad Gamepad { get; init; }
    public InputButton Button { get; init; }
}

public delegate void GamepadButtonStateChangedDelegate(GamepadButtonStateChangedEvent evt);

public interface IGenericGamepad
{
    event GamepadButtonStateChangedDelegate ButtonPressed;
    event GamepadButtonStateChangedDelegate ButtonReleased;
    
    InputButton NorthButton { get; }
    InputButton EastButton { get; }
    InputButton SouthButton { get; }
    InputButton WestButton { get; }

    InputButton DPadUpButton { get; }
    InputButton DPadRightButton { get; }
    InputButton DPadDownButton { get; }
    InputButton DPadLeftButton { get; }
    
    InputButton LeftBumperButton { get; }
    InputButton RightBumperButton { get; }
    
    IEnumerable<InputButton> Buttons { get; }
}