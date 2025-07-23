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

        var fileItem = new MenuItem
        {
            Text = "File"
        };
        fileItem.Controller = new FileMenuItemController(fileItem, contextMenuManager, app);
        
        var editItem = new MenuItem();
        editItem.Controller = new TestMenuItemController(editItem, contextMenuManager, "Edit");

        var viewLabel = new MenuItem();
        viewLabel.Controller = new TestMenuItemController(viewLabel, contextMenuManager, "View");
        
        var specialMenuItem = new MenuItem();
        specialMenuItem.Controller = new SpecialMenuItemController(specialMenuItem);

        var helpLabel = new MenuItem();
        helpLabel.Controller = new TestMenuItemController(helpLabel, contextMenuManager, "Help");
        
        var row = new FlexRow
        {
            MainAxisAlignment = MainAxisAlignment.Start,
            CrossAxisAlignment = CrossAxisAlignment.Stretch,
            Gap = 10,
            Children =
            {
                fileItem,
                editItem,
                viewLabel,
                specialMenuItem,
                helpLabel,
            }
        };
        
        background.Children.Add(row);
        container.Children.Add(background);
        Add(container);
    }
}