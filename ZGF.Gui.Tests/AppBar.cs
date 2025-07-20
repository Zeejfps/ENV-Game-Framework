using ZGF.Gui.Layouts;

namespace ZGF.Gui.Tests;

public sealed class MenuItemController : IMenuItemController
{
    private readonly IMenuItem _menuItem;
    private readonly ContextMenuManager _contextMenuManager;

    private ContextMenu? _contextMenu;

    public MenuItemController(IMenuItem menuItem, string text)
    {
        _menuItem = menuItem;
        _contextMenuManager = menuItem.Context.Get<ContextMenuManager>();
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
    }

    public void OnMouseExit()
    {
        _menuItem.IsHovered = false;

        if (_contextMenu != null)
        {
            _contextMenuManager.HideContextMenu(_contextMenu);
        }
    }
}

public sealed class SpecialMenuItemController : IMenuItemController
{
    public SpecialMenuItemController(IMenuItem menuItem)
    {
        menuItem.Text = "Special";
        menuItem.IsDisabled = true;
    }

    public void Dispose()
    {

    }

    public void OnMouseEnter()
    {

    }

    public void OnMouseExit()
    {
    }
}

public sealed class AppBar : Component
{
    public AppBar()
    {
        var container = new Panel
        {
            BackgroundColor = 0x000000,
            Padding = new PaddingStyle
            {
                Bottom = 1,
            }
        };
        var background = new Panel
        {
            BackgroundColor = 0xDEDEDE,
            BorderSize = BorderSizeStyle.All(1),
            BorderColor = new BorderColorStyle
            {
                Top = 0xFFFFFF,
                Left = 0xFFFFFF,
                Right = 0x9C9C9C,
                Bottom = 0x9C9C9C
            }
        };

        var fileItem = new MenuItem(
            menuItem => new MenuItemController(menuItem, "File")
        );
        var editItem = new MenuItem(
            menuItem => new MenuItemController(menuItem, "Edit")
        );
        var viewLabel = new MenuItem(
            menuItem => new MenuItemController(menuItem, "View")
        );
        var specialLabel = new MenuItem(
            menuItem => new SpecialMenuItemController(menuItem)
        );
        var helpLabel = new MenuItem(
            menuItem => new MenuItemController(menuItem, "Help")
        );
        
        var row = new FlexRow(MainAxisAlignment.Start, CrossAxisAlignment.Stretch, 10)
        {
            fileItem,
            editItem,
            viewLabel,
            specialLabel,
            helpLabel,
        };
        
        background.Add(row);
        container.Add(background);
        Add(container);
    }
}