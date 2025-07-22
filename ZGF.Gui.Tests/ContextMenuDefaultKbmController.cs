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
        _contextMenuManager?.SetKeepOpen(_contextMenu);
        if (_contextMenu.ParentMenu != null)
        {
            _contextMenuManager?.SetKeepOpen(_contextMenu.ParentMenu);
        }
    }

    public void OnMouseExit()
    {
        _contextMenuManager?.HideContextMenu(_contextMenu);
        if (_contextMenu.ParentMenu != null)
        {
            _contextMenuManager?.HideContextMenu(_contextMenu.ParentMenu);
        }
    }

    public Component Component => _contextMenu;
}