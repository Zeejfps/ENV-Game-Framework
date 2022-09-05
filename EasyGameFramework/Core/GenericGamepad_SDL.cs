using EasyGameFramework.Api.InputDevices;

namespace EasyGameFramework.Core;

internal class GenericGamepad_SDL : IGenericGamepad
{
    public event GamepadButtonStateChangedDelegate? ButtonPressed;
    public event GamepadButtonStateChangedDelegate? ButtonReleased;
    
    public GamepadButton NorthButton { get; } = new();
    public GamepadButton EastButton { get; } = new();
    public GamepadButton WestButton { get; } = new();
    public GamepadButton SouthButton { get; } = new();

    public GamepadButton DPadUpButton { get; } = new();
    public GamepadButton DPadRightButton { get; } = new();
    public GamepadButton DPadDownButton { get; } = new();
    public GamepadButton DPadLeftButton { get; } = new();
    public GamepadButton LeftBumperButton { get; } = new();
    public GamepadButton RightBumperButton { get; } = new();

    public IEnumerable<GamepadButton> Buttons { get; } 

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