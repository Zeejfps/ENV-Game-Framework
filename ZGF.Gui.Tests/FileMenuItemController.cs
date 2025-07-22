namespace ZGF.Gui.Tests;

public sealed class FileMenuItemController : IMenuItemController
{
    private readonly App _app;
    private readonly IMenuItem _menuItem;
    private readonly ContextMenuManager _contextMenuManager;

    private ContextMenu? _contextMenu;

    public FileMenuItemController(IMenuItem menuItem, ContextMenuManager contextMenuManager, App app)
    {
        _menuItem = menuItem;
        _contextMenuManager = contextMenuManager;
        _app = app;
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

        var openModelItem = new ContextMenuItem
        {
            Text = "Open Model",
        };
        _contextMenu.AddItem(openModelItem);

        var exitItem = new ContextMenuItem
        {
            Text = "Exit",
        };
        exitItem.AddController(new ContextMenuItemDefaultKbmController(_contextMenu, exitItem, () =>
        {
            _app.Exit();
        }));
        _contextMenu.AddItem(exitItem);
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