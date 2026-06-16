using System.Numerics;
using ZGF.Geometry;
using ZGF.Gui.Desktop.Input;

namespace ZGF.Gui.Desktop.Controllers;

/// <summary>
/// Framework-owned drag state machine, so app code supplies stateless callbacks instead of
/// re-implementing press/track/release. With <see cref="Threshold"/> = 0 the drag starts on
/// left press; otherwise the press arms the recognizer and the drag starts once the cursor
/// travels past the threshold. While dragging it steals focus (so moves keep flowing to it
/// off-view), consumes moves and hover transitions, and reports incremental deltas. Release
/// ends the drag and returns focus. Register via a factory so the gesture state resets per
/// mount.
/// </summary>
public sealed class DragRecognizer : KeyboardMouseController
{
    private readonly InputSystem _input;
    private PointF _prevPoint;
    private bool _isArmed;
    private bool _isDragging;

    public float Threshold { get; init; }
    public Action? DragStarted { get; init; }
    public Action<Vector2>? Dragged { get; init; }
    public Action? DragEnded { get; init; }

    public bool IsDragging => _isDragging;

    public DragRecognizer(InputSystem input)
    {
        _input = input;
    }

    public override void OnMouseEnter(ref MouseEnterEvent e)
    {
        if (e.Phase == EventPhase.Bubbling && _isDragging)
            e.Consume();
    }

    public override void OnMouseExit(ref MouseExitEvent e)
    {
        if (e.Phase == EventPhase.Bubbling && _isDragging)
            e.Consume();
    }

    public override void OnMouseButtonStateChanged(ref MouseButtonEvent e)
    {
        if (e.Phase != EventPhase.Bubbling)
            return;
        if (e.Button != MouseButton.Left)
            return;

        if (e.State == InputState.Pressed && !_isDragging)
        {
            _prevPoint = e.Mouse.Point;
            if (Threshold <= 0f)
            {
                _isDragging = true;
                _input.StealFocus(this);
                DragStarted?.Invoke();
                e.Consume();
            }
            else
            {
                _isArmed = true;
            }
            return;
        }

        if (e.State == InputState.Released)
        {
            _isArmed = false;
            if (_isDragging)
            {
                _isDragging = false;
                _input.Blur(this);
                DragEnded?.Invoke();
            }
        }
    }

    public override void OnMouseMoved(ref MouseMoveEvent e)
    {
        if (e.Phase != EventPhase.Bubbling)
            return;

        if (_isDragging)
        {
            var delta = e.Mouse.Point - _prevPoint;
            _prevPoint = e.Mouse.Point;
            Dragged?.Invoke(delta);
            e.Consume();
            return;
        }

        if (!_isArmed)
            return;
        if (!e.Mouse.IsButtonPressed(MouseButton.Left))
        {
            _isArmed = false;
            return;
        }

        var travel = e.Mouse.Point - _prevPoint;
        if (travel.LengthSquared() > Threshold * Threshold)
        {
            _isArmed = false;
            _isDragging = true;
            _input.StealFocus(this);
            DragStarted?.Invoke();
            e.Consume();
        }
    }
}
