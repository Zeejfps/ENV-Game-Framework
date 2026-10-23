using System.Diagnostics.CodeAnalysis;
using ZGF.Geometry;

namespace ZGF.Gui.Immediate;

public abstract partial class Ui
{
    private const float DragThreshold = 6f;

    private object? _drag;
    private bool _dropping;
    private bool _dragBlocked;

    /// <summary>Where the pointer was when the left button last went down.</summary>
    public PointF PressPosition => _lastPressAt;

    /// <summary>True while something is being dragged, including the frame it is dropped.</summary>
    public bool IsDragging => _drag != null;

    private sealed class DragStart<T>
    {
        public T Value = default!;
    }

    /// <summary>Keeps <paramref name="current"/> as it was on the frame <paramref name="held"/> was
    /// pressed and, on each later frame it is still held, gives it back as <paramref name="start"/>
    /// and returns true: with <see cref="PressPosition"/>, where a drag moves something from.</summary>
    public bool TryGetDragStart<T>(Interaction held, T current, out T start)
    {
        start = current;
        if (!held.Held) return false;
        var state = GetState<DragStart<T>>(held.Id);
        if (held.Pressed)
        {
            state.Value = current;
            return false;
        }
        start = state.Value;
        return true;
    }

    /// <summary>True while <paramref name="held"/> is held, nothing is being dragged yet, and the
    /// pointer has moved far enough from where it was pressed to start a drag — the cue to call
    /// <see cref="BeginDrag"/>. A drag cancelled while the button is down does not start again
    /// until it comes up.</summary>
    public bool ShouldBeginDrag(Interaction held)
    {
        if (!held.Held || held.Pressed || _drag != null || _dragBlocked || !Input.IsDown(PointerButton.Left)) return false;
        var pointer = Input.Pointer;
        var dx = pointer.X - _lastPressAt.X;
        var dy = pointer.Y - _lastPressAt.Y;
        return dx * dx + dy * dy >= DragThreshold * DragThreshold;
    }

    /// <summary>Starts dragging <paramref name="payload"/> while the left button stays down. Every
    /// widget can read it with <see cref="TryGetDrag{T}"/>; the one under the pointer when the button
    /// comes up takes it with <see cref="TryDrop{T}"/>. A drop nobody takes is dropped.</summary>
    public void BeginDrag(object payload)
    {
        _drag = payload;
        _dropping = false;
        RequestFrame();
    }

    /// <summary>The payload being dragged, when it is a <typeparamref name="T"/>.</summary>
    public bool TryGetDrag<T>([NotNullWhen(true)] out T? payload) where T : class
    {
        payload = _drag as T;
        return payload != null;
    }

    /// <summary>On the frame the left button comes up during a drag of a <typeparamref name="T"/>,
    /// takes the payload and ends the drag.</summary>
    public bool TryDrop<T>([NotNullWhen(true)] out T? payload) where T : class
    {
        payload = _dropping ? _drag as T : null;
        if (payload == null) return false;
        _drag = null;
        _dropping = false;
        return true;
    }

    /// <summary>Ends the drag without dropping it anywhere.</summary>
    public void CancelDrag()
    {
        _dragBlocked = _drag != null && !_dropping && _input.IsDown(PointerButton.Left);
        _drag = null;
        _dropping = false;
    }

    private void AdvanceDrag()
    {
        if (!_input.IsDown(PointerButton.Left))
            _dragBlocked = false;
        if (_dropping)
            CancelDrag();
        if (_drag == null) return;
        if (_input.WasCancelled(PointerButton.Left))
            CancelDrag();
        else if (_input.WasReleased(PointerButton.Left) || !_input.IsDown(PointerButton.Left))
            _dropping = true;
    }
}
