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

    public void OnMouseEnter(in MouseEnterEvent e)
    {
        _isHovered = true;
        _view.IsSelected = true;
        this.RequestFocus();
    }

    public void OnMouseExit(in MouseExitEvent e)
    {
        _isHovered = false;
        if (!_isDragging)
        {
            _view.IsSelected = false;
            this.Blur();
        }
    }

    public void OnFocusLost()
    {
        if (_isDragging)
        {
            _isDragging = false;
            _view.IsSelected = false;
        }
    }

    public bool OnMouseButtonStateChanged(in MouseButtonEvent e)
    {
        if (!_isDragging &&
            e.Button == MouseButton.Left &&
            e.State == InputState.Pressed)
        {
            _isDragging = true;
            _startPoint = e.Mouse.Point;
            return false;
        }

        if (_isDragging &&
            e.Button == MouseButton.Left &&
            e.State == InputState.Released)
        {
            _isDragging = false;
            if (!_isHovered)
            {
                _view.IsSelected = false;
                this.Blur();
            }
            return true;
        }

        return false;
    }

    public bool OnMouseMoved(in MouseMoveEvent e)
    {
        if (!_isDragging)
        {
            return false;
        }

        var delta = e.Mouse.Point - _startPoint;
        var deltaY = delta.Y;
        _startPoint =  e.Mouse.Point;

        _view.Move(deltaY);

        return true;
    }

    public bool CanReleaseFocus()
    {
        return !_isDragging;
    }
}