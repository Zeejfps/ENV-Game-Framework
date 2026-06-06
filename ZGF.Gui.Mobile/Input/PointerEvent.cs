using ZGF.Geometry;

namespace ZGF.Gui.Mobile.Input;

/// <summary>
/// A single pointer event delivered to controllers. Unlike the desktop input model — which has
/// a distinct struct per interaction (MouseButtonEvent, MouseMoveEvent, …) — touch has one
/// event shape; the lifecycle stage is conveyed by which <see cref="IPointerController"/> method
/// receives it (OnPointerTouched / OnPointerMoved / OnPointerReleased / …).
/// </summary>
public struct PointerEvent : IPointerEvent
{
    /// <summary>The shared active pointer (current position, down-state).</summary>
    public required Pointer Pointer { get; init; }

    /// <summary>This event's position in GUI coordinates (Y-up), captured when it was raised.</summary>
    public required PointF Position { get; init; }

    public required PointerPhase Phase { get; set; }

    public bool IsConsumed { get; private set; }

    public void Consume() => IsConsumed = true;
}
