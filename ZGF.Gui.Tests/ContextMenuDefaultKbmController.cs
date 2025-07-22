namespace ZGF.Gui.Tests;

public sealed class ContextMenuDefaultKbmController : IKeyboardMouseController
{
    private readonly MenuItem _menuItem;
    private readonly ContextMenu _contextMenu;
    private ContextMenuManager? _contextMenuManager;
    
    public ContextMenuDefaultKbmController(MenuItem menuItem, ContextMenu contextMenu)
    {
        _menuItem = menuItem;
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
        //Console.WriteLine("OnMouseEnter");
        _menuItem.IsHovered = true;
        _contextMenuManager?.SetKeepOpen(_contextMenu);
    }

    public void OnMouseExit()
    {
        //Console.WriteLine("OnMouseExit");
        _menuItem.IsHovered = false;
        _contextMenuManager?.HideContextMenu(_contextMenu);
    }

    public Component Component => _contextMenu;
}