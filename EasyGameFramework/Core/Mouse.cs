using EasyGameFramework.Api.InputDevices;

namespace EasyGameFramework.Core;

internal class Mouse : IMouse
{
    private readonly HashSet<MouseButton> m_ButtonsPressedThisFrame = new();
    private readonly HashSet<MouseButton> m_ButtonsReleasedThisFrame = new();
    private readonly HashSet<MouseButton> m_PressedButtons = new();
    
    public int ScreenX { get; set; }
    public int ScreenY { get; set; }
    public float ScrollDeltaX { get; set; }
    public float ScrollDeltaY { get; set; }

    public void PressButton(MouseButton button)
    {
        if (m_PressedButtons.Add(button))
            m_ButtonsPressedThisFrame.Add(button);
    }

    public void ReleaseButton(MouseButton button)
    {
        if (m_PressedButtons.Remove(button))
            m_ButtonsReleasedThisFrame.Add(button);
    }

    public bool WasButtonPressedThisFrame(MouseButton button)
    {
        return m_ButtonsPressedThisFrame.Contains(button);
    }

    public bool WasButtonReleasedThisFrame(MouseButton button)
    {
        return m_ButtonsReleasedThisFrame.Contains(button);
    }

    public bool IsButtonPressed(MouseButton button)
    {
        return m_PressedButtons.Contains(button);
    }

    public bool IsButtonReleased(MouseButton button)
    {
        return !m_PressedButtons.Contains(button);
    }

    public void Reset()
    {
        ScrollDeltaX = 0;
        ScrollDeltaY = 0;
        m_ButtonsPressedThisFrame.Clear();
        m_ButtonsReleasedThisFrame.Clear();
    }

    public override string ToString()
    {
        return $"{ScreenX}, {ScreenY}";
    }
}