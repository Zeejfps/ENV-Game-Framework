using ZGF.Geometry;

namespace ZGF.Gui.Tests;

public sealed class VerticalScrollBarThumbViewController : IKeyboardMouseController
{
    public View View => _view;

    private readonly VerticalScrollBarThumbView _view;

    private PointF _startPoint;
    private bool _isDragging;
    private bool _isHovered;

    public VerticalScrollBarThumbViewController(VerticalScrollBarThumbView view)
    {
        _view = view;
    }

    public void OnEnabled(Context context)
    {
        context.InputSystem.AddInteractable(this);
    }

    public void OnDisabled(Context context)
    {
        context.InputSystem.RemoveInteractable(this);
    }

    public void OnMouseEnter(ref MouseEnterEvent e)
    {
        if (e.Phase != EventPhase.Bubbling)
            return;
        
        _isHovered = true;
        _view.IsSelected = true;
        e.Consume();
    }

    public void OnMouseExit(ref MouseExitEvent e)
    {
        if (e.Phase != EventPhase.Bubbling)
            return;
        
        _isHovered = false;
        if (!_isDragging)
        {
            _view.IsSelected = false;
        }
        e.Consume();
    }

    public void OnKeyboardKeyStateChanged(ref KeyboardKeyEvent e) { }

    public void OnFocusLost()
    {
    }

    public void OnFocusGained()
    {
    }

    public void OnMouseButtonStateChanged(ref MouseButtonEvent e)
    {
        if (e.Phase != EventPhase.Bubbling)
            return;
        
        if (!_isDragging &&
            e.Button == MouseButton.Left &&
            e.State == InputState.Pressed)
        {
            _isDragging = true;
            _startPoint = e.Mouse.Point;
            this.RequestFocus();
            e.Consume();
            return;
        }

        if (_isDragging &&
            e.Button == MouseButton.Left &&
            e.State == InputState.Released)
        {
            _isDragging = false;
            if (!_isHovered)
            {
                _view.IsSelected = false;
            }
            this.Blur();
            return;
        }
    }

    public void OnMouseWheelScrolled(ref MouseWheelScrolledEvent e)
    {
        
    }

    public void OnMouseMoved(ref MouseMoveEvent e)
    {
        if (e.Phase != EventPhase.Bubbling)
            return;
        
        if (!_isDragging)
        {
            return;
        }

        var delta = e.Mouse.Point - _startPoint;
        var deltaY = delta.Y;
        _startPoint =  e.Mouse.Point;
        _view.Move(deltaY);
        e.Consume();
    }

    public bool CanReleaseFocus()
    {
        return !_isDragging;
    }
}