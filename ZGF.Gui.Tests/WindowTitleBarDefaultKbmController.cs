using ZGF.Geometry;

namespace ZGF.Gui.Tests;

public sealed class WindowTitleBarDefaultKbmController : IKeyboardMouseController
{
    private readonly Window _window;
    private readonly WindowTitleBar _titleBar;
    private PointF _prevMousePosition;
    private bool _isHovered;
    private bool _isDragging;
    
    public WindowTitleBarDefaultKbmController(Window window, WindowTitleBar titleBar)
    {
        _window = window;
        _titleBar = titleBar;
    }

    public void OnEnabled(Context context)
    {
        context.InputSystem.AddInteractable(this);
    }

    public void OnDisabled(Context context)
    {
        context.InputSystem.RemoveInteractable(this);
    }

    public void OnMouseEnter()
    {
        Console.WriteLine($"OnMouseEnter: TitleBar - {_window.TitleText}");
        _isHovered = true;
        this.RequestFocus();
    }

    public void OnMouseExit()
    {
        Console.WriteLine($"OnMouseExit: TitleBar - {_window.TitleText}");
        _isHovered = false;
        if (!_isDragging)
        {
            this.Blur();
        }
    }

    public bool OnMouseButtonStateChanged(in MouseButtonEvent e)
    {
        Console.WriteLine($"OnMouseButtonStateChanged: TitleBar - {_window.TitleText}");
        var button = e.Button;
        var state = e.State;

        if (button != MouseButton.Left)
            return false;

        if (state == InputState.Pressed)
        {
            _prevMousePosition = e.Mouse.Point;
            Console.WriteLine($"OnLeftButtonPressed: TitleBar - {_window.TitleText}");
            return false;
        }
        
        _isDragging = false;
        
        if (!_isHovered)
        {
            Console.WriteLine("Not hovered");
            this.Blur();
        }

        return false;
    }

    public void OnFocusLost()
    {
        _isDragging = false;
    }

    public bool CanReleaseFocus()
    {
        return !_isDragging;
    }

    public bool OnMouseMoved(in MouseMoveEvent e)
    {
        var isLeftButtonPressed = e.Mouse.IsButtonPressed(MouseButton.Left);
        if (!isLeftButtonPressed)
            return false;

        var delta = e.Mouse.Point - _prevMousePosition;
        if (_isDragging)
        {
            _window.Move(delta.X, delta.Y);
            _prevMousePosition = e.Mouse.Point;
            return true;
        }
        
        if (delta.LengthSquared() > 1f)
        {
            _isDragging = true;
            return true;
        }

        return false;
    }
    
    public Component Component => _titleBar;
}