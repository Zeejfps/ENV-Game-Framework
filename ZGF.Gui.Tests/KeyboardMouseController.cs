namespace ZGF.Gui.Tests;

public abstract class KeyboardMouseController : IKeyboardMouseController
{
    
    public virtual void OnEnabled(Context context)
    {
        context.InputSystem.AddInteractable(this);
    }

    public virtual void OnDisabled(Context context)
    {
        context.InputSystem.RemoveInteractable(this);
    }

    public abstract View View { get; }
    

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