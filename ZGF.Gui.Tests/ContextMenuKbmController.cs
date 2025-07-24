namespace ZGF.Gui.Tests;

public sealed class ContextMenuKbmController : IKeyboardMouseController
{
    private readonly IOpenedContextMenu _contextMenu;

    public ContextMenuKbmController(IOpenedContextMenu contextMenu)
    {
        _contextMenu = contextMenu;
    }

    public void OnEnabled(Context context)
    {
        context.InputSystem.AddInteractable(this);
    }

    public void OnDisabled(Context context)
    {
        context.InputSystem.RemoveInteractable(this);
    }
    
    public void OnMouseEnter(ref MouseEnterEvent e)
    {
        _contextMenu.CancelCloseRequest();
    }

    public void OnMouseExit(ref MouseExitEvent e)
    {
        _contextMenu.CloseRequest();
    }

    public void OnMouseButtonStateChanged(ref MouseButtonEvent e)
    {
        
    }

    public void OnMouseWheelScrolled(ref MouseWheelScrolledEvent e)
    {
    }

    public void OnMouseMoved(ref MouseMoveEvent e)
    {
    }

    public void OnKeyboardKeyStateChanged(ref KeyboardKeyEvent e)
    {
    }

    public void OnFocusLost()
    {
    }

    public void OnFocusGained()
    {
    }

    public View View => _contextMenu.View;
}