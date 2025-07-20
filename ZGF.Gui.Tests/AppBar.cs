using ZGF.Gui.Layouts;

namespace ZGF.Gui.Tests;

public sealed class MenuItemController : IMenuItemController
{
    public MenuItemController(string text, IMenuItem menuItem)
    {
        menuItem.Text = text;
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
            menuItem => new MenuItemController("File", menuItem)
        );
        var editItem = new MenuItem(
            menuItem => new MenuItemController("Edit", menuItem)
        );
        var viewLabel = new MenuItem(
            menuItem => new MenuItemController("View", menuItem)
        );
        var specialLabel = new MenuItem(
            menuItem => new MenuItemController("Special", menuItem)
        );
        var helpLabel = new MenuItem(
            menuItem => new MenuItemController("Help", menuItem)
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