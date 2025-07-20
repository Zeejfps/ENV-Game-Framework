namespace ZGF.Gui.Tests;

public sealed class TestMenuItemController : IMenuItemController
{
    private readonly IMenuItem _menuItem;
    private readonly ContextMenuManager _contextMenuManager;

    private ContextMenu? _contextMenu;

    public TestMenuItemController(IMenuItem menuItem, string text, ContextMenuManager contextMenuManager)
    {
        _menuItem = menuItem;
        _contextMenuManager = contextMenuManager;
        menuItem.Text = text;
    }

    public void Dispose()
    {

    }

    public void OnMouseEnter()
    {
        _menuItem.IsHovered = true;
        _contextMenu = _contextMenuManager
            .ShowContextMenu(_menuItem.Position.BottomLeft);

        _contextMenu.AddItem(new ContextMenuItem(_contextMenu)
        {
            Text = "Option 1"
        });
        _contextMenu.AddItem(new ContextMenuItem(_contextMenu)
        {
            Text = "Option 2"
        });
        _contextMenu.AddItem(new ContextMenuItem(_contextMenu)
        {
            Text = "Option 3",
            SubOptions =
            {
                new ContextMenuItemData
                {
                    Text = "Test1"
                },
                new ContextMenuItemData
                {
                    Text = "Test2"
                },
                new ContextMenuItemData
                {
                    Text = "Test3"
                },
            }
        });
        _contextMenu.AddItem(new ContextMenuItem(_contextMenu)
        {
            Text = "Option 4"
        });
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