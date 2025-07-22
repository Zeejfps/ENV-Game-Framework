namespace ZGF.Gui.Tests;

public abstract class KeyboardMouseController<T> : IController where T : Component
{
    private T _component;
    
    public void OnAttachedToContext(Context context)
    {
        // context.InputSystem.AddInteractable(_component, this);
    }

    public void OnDetachedFromContext(Context context)
    {
        context.InputSystem.RemoveInteractable(_component);
    }
    
    public void HandleMouseEnterEvent()
    {
        OnMouseEnter();
    }

    public void HandleMouseExitEvent()
    {
        OnMouseExit();
    }

    public bool HandleMouseButtonEvent(in MouseButtonEvent e)
    {
        return OnMouseButtonStateChanged(e);
    }

    public void HandleMouseWheelEvent()
    {
    }

    public bool HandleMouseMoveEvent(in MouseMoveEvent e)
    {
        return OnMouseMoved(e);
    }
    
    protected virtual void OnMouseEnter(){}
    protected virtual void OnMouseExit(){}
    protected virtual bool OnMouseMoved(MouseMoveEvent e) { return true; }
    protected virtual bool OnMouseButtonStateChanged(MouseButtonEvent e) { return false; }
}