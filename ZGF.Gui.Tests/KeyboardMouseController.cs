namespace ZGF.Gui.Tests;

public abstract class KeyboardMouseController<T> : IKeyboardMouseController where T : Component
{
    private readonly T _component;
    private Context? _context;
    
    public void OnEnabled(Context context)
    {
        _context = context;
        // context.InputSystem.AddInteractable(_component, this);
    }

    public void OnDisabled(Context context)
    {
        context.InputSystem.RemoveInteractable(_component);
    }

    protected void RequestFocus()
    {
        _context?.InputSystem.RequestFocus(_component);
    }

    protected void Blur()
    {
        _context?.InputSystem.Blur(_component);
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