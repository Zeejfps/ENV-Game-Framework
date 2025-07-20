namespace ZGF.Gui.Tests;

public sealed class FileMenuItemController : IMenuItemController
{
    private readonly IMenuItem _menuItem;
    private readonly ContextMenuManager _contextMenuManager;

    private ContextMenu? _contextMenu;

    public FileMenuItemController(IMenuItem menuItem, ContextMenuManager contextMenuManager)
    {
        _menuItem = menuItem;
        _contextMenuManager = contextMenuManager;
        menuItem.Text = "File";
    }

    public void Dispose()
    {

    }

    public void OnMouseEnter()
    {
        _menuItem.IsHovered = true;
        _contextMenu = _contextMenuManager
            .ShowContextMenu(_menuItem.Position.BottomLeft);
        _contextMenu.AddItem(new ContextMenuItem(_contextMenu, "Exit"));
    }

    public void OnMouseExit()
    {
        _menuItem.IsHovered = false;

        if (_contextMenu != null)
        {
            _contextMenuManager.HideContextMenu(_contextMenu);
            _contextMenu = null;
        }
    }
}