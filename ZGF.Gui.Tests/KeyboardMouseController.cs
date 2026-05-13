namespace ZGF.Gui.Tests;

public abstract class KeyboardMouseController : IKeyboardMouseController, IViewBehavior
{
    protected virtual EventPhaseFilter PhaseFilter => EventPhaseFilter.Both;

    public virtual void OnAttachedToContext(View view, Context context)
    {
        context.Get<InputSystem>()!.RegisterController(view, this, PhaseFilter);
    }

    public virtual void OnDetachedFromContext(View view, Context context)
    {
        context.Get<InputSystem>()?.UnregisterController(view);
    }

    public virtual void OnAttached()
    {
    }

    public virtual void OnDetached()
    {
    }

    public virtual void OnMouseEnter(ref MouseEnterEvent e)
    {
    }

    public virtual void OnMouseExit(ref MouseExitEvent e)
    {
    }

    public virtual void OnMouseButtonStateChanged(ref MouseButtonEvent e)
    {
    }

    public virtual void OnMouseWheelScrolled(ref MouseWheelScrolledEvent e)
    {
    }

    public virtual void OnMouseMoved(ref MouseMoveEvent e)
    {
    }

    public virtual void OnKeyboardKeyStateChanged(ref KeyboardKeyEvent e)
    {
    }

    public virtual void OnFocusLost()
    {
    }

    public virtual void OnFocusGained()
    {
    }
}