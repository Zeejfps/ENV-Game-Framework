using System.Numerics;
using EasyGameFramework.Api.Events;
using EasyGameFramework.Api.InputDevices;
using Raylib_CsLo;
using MouseButton = EasyGameFramework.Api.InputDevices.MouseButton;

namespace Bricks.RaylibBackend;

public sealed class RaylibMouse : IMouse
{
    public event MouseMovedDelegate? Moved;
    public event MouseWheelScrolledDelegate? Scrolled;
    public event MouseButtonStateChangedDelegate? ButtonPressed;
    public event MouseButtonStateChangedDelegate? ButtonReleased;
    public event MouseButtonStateChangedDelegate? ButtonStateChanged;
    
    public int ScreenX { get; private set; }
    public int ScreenY { get; private set; }
    
    public void PressButton(MouseButton button)
    {
        throw new NotImplementedException();
    }

    public void ReleaseButton(MouseButton button)
    {
        throw new NotImplementedException();
    }

    public void MoveTo(int viewportX, int viewportY)
    {
        throw new NotImplementedException();
    }

    public void MoveBy(int dx, int dy)
    {
        throw new NotImplementedException();
    }

    public void Scroll(float dx, float dy)
    {
        throw new NotImplementedException();
    }

    public bool IsButtonPressed(MouseButton button)
    {
        return _isPressed;
    }

    public Vector2 ToWorldCoords(int x, int y)
    {
        return new Vector2(x, y);
    }

    private bool _isPressed;

    public void Update()
    {
        var prevMousePosX = ScreenX;
        var prevMousePosY = ScreenY;
        ScreenX = Raylib.GetMouseX();
        ScreenY = Raylib.GetMouseY();

        if (ScreenX != prevMousePosX || ScreenY != prevMousePosY)
        {
            Moved?.Invoke(new MouseMovedEvent
            {
                Mouse = this,
                DeltaX = ScreenX - prevMousePosX,
                DeltaY = ScreenY - prevMousePosY
            });
        }

        var wasPressed = _isPressed; 
        _isPressed = Raylib.IsMouseButtonDown(Raylib.MOUSE_LEFT_BUTTON);

        if (wasPressed != _isPressed)
        {
            ButtonStateChanged?.Invoke(new MouseButtonStateChangedEvent
            {
                Button = MouseButton.Left,
                Mouse = this,
            });

            if (_isPressed)
            {
                ButtonPressed?.Invoke(new MouseButtonStateChangedEvent
                {
                    Mouse = this,
                    Button = MouseButton.Left
                });
            }
        }
    }
}