using EasyGameFramework.Api.Events;

namespace EasyGameFramework.Api.InputDevices;

public delegate void MouseMovedDelegate(in MouseMovedEvent evt);
public delegate void MouseButtonPressedDelegate(in MouseButtonPressedEvent evt);

public interface IMouse
{
    event MouseMovedDelegate Moved;
    event MouseButtonPressedDelegate ButtonPressed;
    
    int ScreenX { get; set; }
    int ScreenY { get; set; }

    float ScrollDeltaX { get; set; }
    float ScrollDeltaY { get; set; }

    void PressButton(MouseButton button);
    void ReleaseButton(MouseButton button);
    bool WasButtonPressedThisFrame(MouseButton button);
    bool WasButtonReleasedThisFrame(MouseButton button);
    bool IsButtonPressed(MouseButton button);
    bool IsButtonReleased(MouseButton button);

    void SetPosition(int screenX, int screenY);
    
    void Reset();
}