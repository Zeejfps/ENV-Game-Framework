using EasyGameFramework.Api;
using EasyGameFramework.Api.Events;
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

    private IEventBus EventBus { get; }

    public Mouse(IEventBus eventBus)
    {
        EventBus = eventBus;
    }
    
    public void PressButton(MouseButton button)
    {
        if (!m_PressedButtons.Add(button))
            return;
        
        m_ButtonsPressedThisFrame.Add(button);
        EventBus.Publish(new MouseButtonPressedEvent
        {
            Mouse = this,
            Button = button
        });
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

    public void SetPosition(int screenX, int screenY)
    {
        ScreenX = screenX;
        ScreenY = screenY;
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