using ZGF.Geometry;
using ZGF.Gui.Desktop.Controllers;
using ZGF.Gui.Desktop.Input;

namespace ZGF.Gui.Desktop.Components.HorizontalScrollBar;

public sealed class HorizontalScrollBarThumbViewController : KeyboardMouseController
{
    private readonly HorizontalScrollBarThumbView _view;
    private readonly InputSystem _inputSystem;

    private PointF _startPoint;
    private bool _isDragging;
    private bool _isHovered;

    public HorizontalScrollBarThumbViewController(HorizontalScrollBarThumbView view, InputSystem inputSystem)
    {
        _view = view;
        _inputSystem = inputSystem;
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

        var inputSystem = _inputSystem;

        if (!_isDragging &&
            e.Button == MouseButton.Left &&
            e.State == InputState.Pressed)
        {
            _isDragging = true;
            _startPoint = e.Mouse.Point;
            inputSystem?.StealFocus(this);
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
            inputSystem?.Blur(this);
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
        var deltaX = delta.X;
        _startPoint = e.Mouse.Point;
        _view.Move(deltaX);
        e.Consume();
    }

    public bool CanReleaseFocus()
    {
        return !_isDragging;
    }
}
