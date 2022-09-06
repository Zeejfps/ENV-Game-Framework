using EasyGameFramework.Api.Events;
using EasyGameFramework.Api.InputDevices;

namespace EasyGameFramework.Core;

internal class Gamepad_SDL : IGamepad
{
    public event GamepadButtonStateChangedDelegate? ButtonPressed;
    public event GamepadButtonStateChangedDelegate? ButtonReleased;
    
    private string Name { get; }
    private string Guid { get; }

    private readonly HashSet<GamepadButton> m_PressedButtons = new();

    public Gamepad_SDL(string guid, string name)
    {
        Guid = guid;
        Name = name;
    }
    
    public void PressButton(GamepadButton button)
    {
        if (!m_PressedButtons.Add(button))
            return;
        
        ButtonPressed?.Invoke(new GamepadButtonStateChangedEvent
        {
            Button = button,
            Gamepad = this,
        });
    }

    public void ReleaseButton(GamepadButton button)
    {
        if (!m_PressedButtons.Remove(button))
            return;
        
        ButtonReleased?.Invoke(new GamepadButtonStateChangedEvent
        {
            Button = button,
            Gamepad = this,
        });
    }

    public bool IsButtonPressed(GamepadButton button)
    {
        return m_PressedButtons.Contains(button);
    }

    public override string ToString()
    {
        return Name;
    }
}