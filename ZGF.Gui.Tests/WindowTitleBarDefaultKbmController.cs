using ZGF.Geometry;

namespace ZGF.Gui.Tests;

public sealed class WindowTitleBarDefaultKbmController : KeyboardMouseController
{
    private readonly Window _window;
    private readonly WindowTitleBarView _titleBar;
    private PointF _prevMousePosition;
    private bool _isDragging;
    
    public WindowTitleBarDefaultKbmController(Window window, WindowTitleBarView titleBar)
    {
        _window = window;
        _titleBar = titleBar;
    }

    public override void OnMouseEnter(ref MouseEnterEvent e)
    {
        if (e.Phase != EventPhase.Bubbling)
            return;

        if (_isDragging)
            e.Consume();
    }

    public override void OnMouseExit(ref MouseExitEvent e)
    {
        if (e.Phase != EventPhase.Bubbling)
            return;
       
        if (_isDragging)
            e.Consume();
    }

    public override void OnMouseButtonStateChanged(ref MouseButtonEvent e)
    {
        if (e.Phase != EventPhase.Bubbling)
            return;
        
        var button = e.Button;
        var state = e.State;

        if (button != MouseButton.Left)
            return;

        if (state == InputState.Pressed)
        {
            _prevMousePosition = e.Mouse.Point;
            return;
        }
        
        _isDragging = false;
        this.Blur();
    }

    public bool CanReleaseFocus()
    {
        return !_isDragging;
    }

    public override void OnMouseMoved(ref MouseMoveEvent e)
    {
        if (e.Phase != EventPhase.Bubbling)
            return;
        
        var isLeftButtonPressed = e.Mouse.IsButtonPressed(MouseButton.Left);
        if (!isLeftButtonPressed)
            return;

        var delta = e.Mouse.Point - _prevMousePosition;
        if (_isDragging)
        {
            _window.Move(delta.X, delta.Y);
            _prevMousePosition = e.Mouse.Point;
            e.Consume();
            return;
        }
        
        if (delta.LengthSquared() > 1f)
        {
            _isDragging = true;
            e.Consume();
            this.RequestFocus();
            return;
        }
    }
    
    public override View View => _titleBar;
}