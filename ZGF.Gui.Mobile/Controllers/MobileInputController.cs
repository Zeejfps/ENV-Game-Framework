using ZGF.Gui.Mobile.Input;

namespace ZGF.Gui.Mobile.Controllers;

/// <summary>
/// Base class for touch controllers, the mobile parallel to KeyboardMouseController: every
/// pointer callback is a virtual no-op so a subclass overrides only what it needs.
/// </summary>
public abstract class MobileInputController : IPointerController
{
    public virtual void OnPointerEntered(ref PointerEvent e)
    {
    }

    public virtual void OnPointerTouched(ref PointerEvent e)
    {
    }

    public virtual void OnPointerMoved(ref PointerEvent e)
    {
    }

    public virtual void OnPointerReleased(ref PointerEvent e)
    {
    }

    public virtual void OnPointerExited(ref PointerEvent e)
    {
    }
}
