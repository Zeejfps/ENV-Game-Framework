namespace ZGF.Gui.Tests;

public sealed class TestMenuItemController : IKeyboardMouseController
{
    private readonly MenuItem _menuItem;
    private readonly ContextMenuManager _contextMenuManager;

    private ContextMenu? _contextMenu;

    public TestMenuItemController(MenuItem menuItem, string text, ContextMenuManager contextMenuManager)
    {
        _menuItem = menuItem;
        _contextMenuManager = contextMenuManager;
        menuItem.Text = text;
    }

    public Component Component => _menuItem;

    public void OnMouseEnter()
    {
        _menuItem.IsHovered = true;
        _contextMenu = _contextMenuManager
            .ShowContextMenu(_menuItem.Position.BottomLeft);

        _contextMenu.AddItem(new ContextMenuItem
        {
            Text = "Option 1"
        });
        _contextMenu.AddItem(new ContextMenuItem
        {
            Text = "Option 2"
        });

        var option3Menu = new ContextMenuItem
        {
            Text = "Option 3"
        };
        option3Menu.AddController(new ContextMenuItemDefaultKbmController(_contextMenu, option3Menu)
        {
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
        
        _contextMenu.AddItem(option3Menu);
        _contextMenu.AddItem(new ContextMenuItem
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

    public void OnEnabled(Context context)
    {
        this.RegisterController(context);
    }

    public void OnDisabled(Context context)
    {
        this.UnregisterController(context);
    }
}