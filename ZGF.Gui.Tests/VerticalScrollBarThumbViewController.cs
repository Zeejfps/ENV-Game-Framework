using ZGF.Geometry;

namespace ZGF.Gui.Tests;

public sealed class VerticalScrollBarThumbViewController : KeyboardMouseController
{
    public override View View => _view;

    private readonly VerticalScrollBarThumbView _view;

    private PointF _startPoint;
    private bool _isDragging;
    private bool _isHovered;

    public VerticalScrollBarThumbViewController(VerticalScrollBarThumbView view)
    {
        _view = view;
    }

    public override void OnMouseEnter(ref MouseEnterEvent e)
    {
        if (e.Phase != EventPhase.Bubbling)
            return;
        
        _isHovered = true;
        _view.IsSelected = true;
        e.Consume();
    }

    public override void OnMouseExit(ref MouseExitEvent e)
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

    public override void OnMouseButtonStateChanged(ref MouseButtonEvent e)
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
    
    public override void OnMouseMoved(ref MouseMoveEvent e)
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