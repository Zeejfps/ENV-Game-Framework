using EasyGameFramework.Api.Events;
using EasyGameFramework.Api.InputDevices;

namespace EasyGameFramework.Core;

public sealed class Gamepad_SDL : IGamepad
{
    public event GamepadButtonStateChangedDelegate? ButtonPressed;
    public event GamepadButtonStateChangedDelegate? ButtonReleased;
    
    private string Name { get; }
    private string Guid { get; }

    private readonly HashSet<GamepadButton> m_PressedButtons = new();
    private readonly Dictionary<GamepadAxis, float> m_AxisToValueMap = new();

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

    public void SetAxisValue(GamepadAxis axis, float value)
    {
        m_AxisToValueMap[axis] = value;
    }

    public float GetAxisValue(GamepadAxis axis)
    {
        if (!m_AxisToValueMap.TryGetValue(axis, out var value))
            value = 0f;
        return value;
    }

    public override string ToString()
    {
        return Name;
    }
}