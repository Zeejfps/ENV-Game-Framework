namespace EasyGameFramework.Api.InputDevices;

public readonly struct GamepadButtonStateChangedEvent
{
    public IGenericGamepad Gamepad { get; init; }
    public GamepadButton Button { get; init; }
}

public delegate void GamepadButtonStateChangedDelegate(GamepadButtonStateChangedEvent evt);

public interface IGenericGamepad
{
    event GamepadButtonStateChangedDelegate ButtonPressed;
    event GamepadButtonStateChangedDelegate ButtonReleased;
    
    GamepadButton NorthButton { get; }
    GamepadButton EastButton { get; }
    GamepadButton SouthButton { get; }
    GamepadButton WestButton { get; }

    GamepadButton DPadUpButton { get; }
    GamepadButton DPadRightButton { get; }
    GamepadButton DPadDownButton { get; }
    GamepadButton DPadLeftButton { get; }
    
    GamepadButton LeftBumperButton { get; }
    GamepadButton RightBumperButton { get; }
    
    IEnumerable<GamepadButton> Buttons { get; }
}