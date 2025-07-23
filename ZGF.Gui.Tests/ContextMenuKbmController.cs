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
    
    public void OnMouseEnter()
    {
        _contextMenu.CancelCloseRequest();
    }

    public void OnMouseExit()
    {
        _contextMenu.Close();
    }

    public Component Component => _contextMenu.Component;
}