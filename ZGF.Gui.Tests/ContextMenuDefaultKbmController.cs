namespace ZGF.Gui.Tests;

public sealed class ContextMenuDefaultKbmController : IKeyboardMouseController
{
    private readonly ContextMenu _contextMenu;
    private ContextMenuManager? _contextMenuManager;
    
    public ContextMenuDefaultKbmController(ContextMenu contextMenu)
    {
        _contextMenu = contextMenu;
    }

    public void OnEnabled(Context context)
    {
        context.InputSystem.AddInteractable(this);
        _contextMenuManager = context.Get<ContextMenuManager>();
    }

    public void OnDisabled(Context context)
    {
        context.InputSystem.RemoveInteractable(this);
        _contextMenuManager = null;
    }
    
    public void OnMouseEnter()
    {
        Console.WriteLine("OnMouseEnter");
        _contextMenuManager?.SetKeepOpen(_contextMenu);
    }

    public void OnMouseExit()
    {
        Console.WriteLine("OnMouseExit");
        _contextMenuManager?.HideContextMenu(_contextMenu);
    }

    public Component Component => _contextMenu;
}