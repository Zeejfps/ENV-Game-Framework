namespace ZGF.Gui.Desktop.Input;

public struct MouseWheelScrolledEvent : IEvent
{
    public required IMouse Mouse { get; init; }
    public required float DeltaX { get; init; }
    public required float DeltaY { get; init; }
    public required EventPhase Phase { get; set; }

    /// <summary>
    /// Where this event sits in the user's own scrolling gesture — fingers down, moving, lifted. Optional
    /// (defaults to <see cref="ScrollPhase.None"/>) because only a precise input device on a platform that
    /// reports it fills this in; a mouse wheel never does.
    /// </summary>
    public ScrollPhase GesturePhase { get; init; }

    /// <summary>
    /// Where this event sits in the *inertial* scrolling that continues after the fingers lift. Anything
    /// other than <see cref="ScrollPhase.None"/> means the operating system generated this event, not the
    /// user — see <see cref="IsMomentum"/>.
    /// </summary>
    public ScrollPhase MomentumPhase { get; init; }

    /// <summary>
    /// True when this event is inertial follow-through rather than the user actively scrolling. A consumer
    /// that treats each gesture as one action (a carousel stepping one item, a page flipping once) wants to
    /// ignore these: they can arrive for a second or more after the fingers lift, so timing alone can't
    /// tell them apart from a fresh swipe.
    /// </summary>
    public bool IsMomentum => MomentumPhase != ScrollPhase.None;

    public bool IsConsumed { get; private set; }
    
    public void Consume()
    {
        IsConsumed = true;
    }
}