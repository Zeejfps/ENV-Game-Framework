namespace ZGF.Gui.Tests;

public abstract class KeyboardMouseController : IKeyboardMouseController, IViewBehavior
{
    protected virtual EventPhaseFilter PhaseFilter => EventPhaseFilter.Both;

    protected Context? Context { get; private set; }
    
    public void AttachToContext(MultiChildView view, Context context)
    {
        Context = context;
        context.Get<InputSystem>()!.RegisterController(view, this, PhaseFilter);
        OnAttachedToContext(view, context);
    }

    public void DetachFromContext(MultiChildView view, Context context)
    {
        OnDetachedFromContext(view, context);
        context.Get<InputSystem>()?.UnregisterController(view);
        Context = null;
    }

    protected virtual void OnAttachedToContext(MultiChildView view, Context context)
    {
    }

    protected virtual void OnDetachedFromContext(MultiChildView view, Context context)
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