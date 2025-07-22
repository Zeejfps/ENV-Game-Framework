using ZGF.Gui.Layouts;

namespace ZGF.Gui.Tests;

public sealed class AppBar : Component
{
    public AppBar(App app, ContextMenuManager contextMenuManager)
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
            Padding = PaddingStyle.All(6),
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
            menuItem => new FileMenuItemController(menuItem, contextMenuManager, app)
        );
        var editItem = new MenuItem(
            menuItem => new TestMenuItemController(menuItem, "Edit", contextMenuManager)
        );
        var viewLabel = new MenuItem(
            menuItem => new TestMenuItemController(menuItem, "View", contextMenuManager)
        );
        
        var specialMenuItem = new MenuItem();
        specialMenuItem.AddController(new SpecialMenuItemController(specialMenuItem));
        
        var helpLabel = new MenuItem(
            menuItem => new TestMenuItemController(menuItem, "Help", contextMenuManager)
        );
        
        var row = new FlexRow(MainAxisAlignment.Start, CrossAxisAlignment.Stretch, 10)
        {
            fileItem,
            editItem,
            viewLabel,
            specialMenuItem,
            helpLabel,
        };
        
        background.Add(row);
        container.Add(background);
        Add(container);
    }
}