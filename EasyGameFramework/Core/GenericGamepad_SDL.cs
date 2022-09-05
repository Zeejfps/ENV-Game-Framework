using EasyGameFramework.Api.InputDevices;

namespace EasyGameFramework.Core;

internal class GenericGamepad_SDL : IGenericGamepad
{
    public event GamepadButtonStateChangedDelegate? ButtonPressed;
    public event GamepadButtonStateChangedDelegate? ButtonReleased;
    public InputButton NorthButton { get; } = new();
    public InputButton EastButton { get; } = new();
    public InputButton WestButton { get; } = new();
    public InputButton SouthButton { get; } = new();

    public InputButton DPadUpButton { get; } = new();
    public InputButton DPadRightButton { get; } = new();
    public InputButton DPadDownButton { get; } = new();
    public InputButton DPadLeftButton { get; } = new();

    public IEnumerable<InputButton> Buttons { get; } 

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