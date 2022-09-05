namespace EasyGameFramework.Api.InputDevices;

public readonly struct GamepadButtonStateChangedEvent
{
    public IGenericGamepad Gamepad { get; init; }
    public GamepadButtonOld Button { get; init; }
}

public delegate void GamepadButtonStateChangedDelegate(GamepadButtonStateChangedEvent evt);

public interface IGenericGamepad
{
    event GamepadButtonStateChangedDelegate ButtonPressed;
    event GamepadButtonStateChangedDelegate ButtonReleased;
    
    GamepadButtonOld NorthButton { get; }
    GamepadButtonOld EastButton { get; }
    GamepadButtonOld SouthButton { get; }
    GamepadButtonOld WestButton { get; }

    GamepadButtonOld DPadUpButton { get; }
    GamepadButtonOld DPadRightButton { get; }
    GamepadButtonOld DPadDownButton { get; }
    GamepadButtonOld DPadLeftButton { get; }
    
    GamepadButtonOld LeftBumperButton { get; }
    GamepadButtonOld RightBumperButton { get; }
    
    IEnumerable<GamepadButtonOld> Buttons { get; }
}