using EasyGameFramework.Api.Events;

namespace EasyGameFramework.Api.InputDevices;

public delegate void MouseMovedDelegate(in MouseMovedEvent evt);
public delegate void MouseWheelScrolledDelegate(in MouseWheelScrolledEvent evt);
public delegate void MouseButtonStateChangedDelegate(in MouseButtonStateChangedEvent evt);

public interface IMouse
{
    event MouseMovedDelegate Moved;
    event MouseWheelScrolledDelegate Scrolled;
    event MouseButtonStateChangedDelegate ButtonPressed;
    event MouseButtonStateChangedDelegate ButtonReleased;
    event MouseButtonStateChangedDelegate ButtonStateChanged;
    
    int ViewportX { get; }
    int ViewportY { get; }

    void PressButton(MouseButton button);
    void ReleaseButton(MouseButton button);

    void MoveTo(int viewportX, int viewportY);
    void MoveBy(int dx, int dy);
    void Scroll(float dx, float dy);
    
    bool IsButtonPressed(MouseButton button);
}