using ZGF.Geometry;

namespace ZGF.Gui.Tests;

public sealed class VerticalScrollBarThumbViewController : IKeyboardMouseController
{
    public View View => _view;

    private readonly VerticalScrollBarThumbView _view;

    private PointF _startPoint;
    private bool _isDragging;

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
        _view.IsSelected = true;
        this.RequestFocus();
    }

    public void OnMouseExit(in MouseExitEvent e)
    {
        if (!_isDragging)
        {
            _view.IsSelected = false;
            this.Blur();
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
            return true;
        }

        if (_isDragging &&
            e.Button == MouseButton.Left &&
            e.State == InputState.Released)
        {
            _isDragging = false;
            _view.IsSelected = false;
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