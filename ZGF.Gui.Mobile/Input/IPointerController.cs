namespace ZGF.Gui.Mobile.Input;

/// <summary>
/// A view's touch behaviour, the mobile parallel to IKeyboardMouseController. A controller is
/// registered against a view in <see cref="MobileInputSystem"/> and receives the pointer
/// lifecycle for gestures that begin over that view.
///
/// Lifecycle for one finger: <see cref="OnPointerEntered"/> + <see cref="OnPointerTouched"/> on
/// touch-down, zero or more <see cref="OnPointerMoved"/> while it drags, then
/// <see cref="OnPointerReleased"/> + <see cref="OnPointerExited"/> on lift (or just
/// <see cref="OnPointerExited"/> if the gesture is cancelled). Because touch captures to its
/// down-target, move/release reach the same controller even if the finger strays outside the
/// view — check <see cref="PointerEvent.Position"/> against the view bounds when that matters
/// (see ButtonPointerController).
/// </summary>
public interface IPointerController
{
    void OnPointerEntered(ref PointerEvent e);
    void OnPointerTouched(ref PointerEvent e);
    void OnPointerMoved(ref PointerEvent e);
    void OnPointerReleased(ref PointerEvent e);
    void OnPointerExited(ref PointerEvent e);
}
