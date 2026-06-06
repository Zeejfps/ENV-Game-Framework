namespace ZGF.Gui.Mobile.Input;

/// <summary>
/// Common surface for pointer events, paralleling the desktop IEvent. A controller calls
/// <see cref="Consume"/> to stop the event propagating further along the capture/bubble path.
/// </summary>
public interface IPointerEvent
{
    PointerPhase Phase { get; set; }
    bool IsConsumed { get; }

    void Consume();
}
