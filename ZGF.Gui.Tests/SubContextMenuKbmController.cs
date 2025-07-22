namespace ZGF.Gui.Tests;

public sealed class SubContextMenuKbmController : IKeyboardMouseController
{
    private readonly ContextMenuItem _contextMenuItem;
    private readonly ContextMenu _parentContextMenu;
    private readonly ContextMenu _contextMenu;
    private readonly ContextMenuManager _contextMenuManager;

    public SubContextMenuKbmController(
        ContextMenuItem contextMenuItem,
        ContextMenu contextMenu,
        ContextMenu parentContextMenu,
        ContextMenuManager contextMenuManager)
    {
        _contextMenuItem = contextMenuItem;
        _contextMenu = contextMenu;
        _parentContextMenu = parentContextMenu;
        _contextMenuManager = contextMenuManager;
    }

    public void OnEnabled(Context context)
    {
        Console.WriteLine("OnEnabled");
        this.RegisterController(context);
    }

    public void OnDisabled(Context context)
    {
        Console.WriteLine("OnDisabled");
        this.UnregisterController(context);
    }

    public void OnMouseEnter()
    {
        Console.WriteLine("OnMouseEnter");
        _contextMenuItem.IsSelected = true;
        _contextMenuManager.SetKeepOpen(_parentContextMenu);
        _contextMenuManager.SetKeepOpen(_contextMenu);
    }

    public void OnMouseExit()
    { 
        Console.WriteLine("OnMouseExit");
        _contextMenuItem.IsSelected = false;
        _contextMenuManager.HideContextMenu(_parentContextMenu);
        _contextMenuManager.HideContextMenu(_contextMenu);
    }

    public Component Component => _contextMenu;
}