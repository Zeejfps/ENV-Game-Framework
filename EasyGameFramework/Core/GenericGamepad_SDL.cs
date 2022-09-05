using EasyGameFramework.Api.InputDevices;

namespace EasyGameFramework.Core;

internal class GenericGamepad_SDL : IGenericGamepad
{
    public event GamepadButtonStateChangedDelegate? ButtonPressed;
    public event GamepadButtonStateChangedDelegate? ButtonReleased;
    
    public GamepadButtonOld NorthButton { get; } = new();
    public GamepadButtonOld EastButton { get; } = new();
    public GamepadButtonOld WestButton { get; } = new();
    public GamepadButtonOld SouthButton { get; } = new();

    public GamepadButtonOld DPadUpButton { get; } = new();
    public GamepadButtonOld DPadRightButton { get; } = new();
    public GamepadButtonOld DPadDownButton { get; } = new();
    public GamepadButtonOld DPadLeftButton { get; } = new();
    public GamepadButtonOld LeftBumperButton { get; } = new();
    public GamepadButtonOld RightBumperButton { get; } = new();

    public IEnumerable<GamepadButtonOld> Buttons { get; } 

    private string Name { get; }
    private string Guid { get; }

    public GenericGamepad_SDL(string guid, string name)
    {
        Guid = guid;
        Name = name;
        
        Buttons = new[]
        {
            NorthButton,
            EastButton,
            WestButton,
            SouthButton,
            DPadUpButton,
            DPadRightButton,
            DPadLeftButton,
            DPadDownButton,
            LeftBumperButton,
            RightBumperButton,
        };

        foreach (var button in Buttons)
        {
            button.Pressed += OnButtonPressed;
            button.Released += OnButtonReleased;
        }
    }

    private void OnButtonPressed(InputButtonStateChangedEvent evt)
    {
        ButtonPressed?.Invoke(new GamepadButtonStateChangedEvent
        {
            Button = evt.Button,
            Gamepad = this,
        });
    }

    private void OnButtonReleased(InputButtonStateChangedEvent evt)
    {
        ButtonReleased?.Invoke(new GamepadButtonStateChangedEvent
        {
            Button = evt.Button,
            Gamepad = this,
        });
    }

    public override string ToString()
    {
        return Name;
    }
}