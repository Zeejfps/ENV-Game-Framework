using EasyGameFramework.Api.Events;
using EasyGameFramework.Api.InputDevices;

namespace EasyGameFramework.Core.InputDevices;

public sealed class Mouse : IMouse
{
    public event MouseMovedDelegate? Moved;
    public event MouseWheelScrolledDelegate? Scrolled;
    public event MouseButtonStateChangedDelegate? ButtonPressed;
    public event MouseButtonStateChangedDelegate? ButtonReleased;
    public event MouseButtonStateChangedDelegate? ButtonStateChanged;

    public int ScreenX { get; private set; }
    public int ScreenY { get; private set; }

    private readonly HashSet<MouseButton> m_PressedButtons = new();

    public void PressButton(MouseButton button)
    {
        if (!m_PressedButtons.Add(button))
            return;
        OnButtonPressed(button);
    }

    public void ReleaseButton(MouseButton button)
    {
        if (!m_PressedButtons.Remove(button))
            return;
        OnButtonReleased(button);
    }
    
    public bool IsButtonPressed(MouseButton button)
    {
        return m_PressedButtons.Contains(button);
    }

    public void MoveTo(int viewportX, int viewportY)
    {
        var deltaX = viewportX - ScreenX;
        var deltaY = viewportY - ScreenY;
        
        if (deltaX == 0 && deltaY == 0)
            return;
        
        ScreenX = viewportX;
        ScreenY = viewportY;
        Moved?.Invoke(new MouseMovedEvent
        {
            Mouse = this,
            DeltaX = deltaX,
            DeltaY = deltaY,
        });
    }

    public void MoveBy(int dx, int dy)
    {
        MoveTo(ScreenX + dx, ScreenY + dy);
    }

    public void Scroll(float dx, float dy)
    {
        Scrolled?.Invoke(new MouseWheelScrolledEvent
        {
            Mouse = this,
            DeltaX = dx,
            DeltaY = dy,
        });
    }

    public override string ToString()
    {
        return $"{ScreenX}, {ScreenY}";
    }

    private void OnButtonPressed(MouseButton button)
    {
        var evt = new MouseButtonStateChangedEvent
        {
            Button = button,
            Mouse = this,
        };
        
        ButtonStateChanged?.Invoke(evt);
        ButtonPressed?.Invoke(evt);
    }

    private void OnButtonReleased(MouseButton button)
    {
        var evt = new MouseButtonStateChangedEvent
        {
            Button = button,
            Mouse = this,
        };
        
        ButtonStateChanged?.Invoke(evt);
        ButtonReleased?.Invoke(evt);
    }
}