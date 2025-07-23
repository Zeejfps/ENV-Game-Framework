using ZGF.Gui.Layouts;

namespace ZGF.Gui.Tests;

public sealed class AppBar : Component
{
    public AppBar(App app, ContextMenuManager contextMenuManager)
    {
        var fileItem = new MenuItem
        {
            Text = "File"
        };
        fileItem.Controller = new FileMenuItemController(fileItem, contextMenuManager, app);
        
        var editItem = new MenuItem
        {
            Text = "Edit"
        };
        editItem.Controller = new TestMenuItemController(editItem, contextMenuManager);

        var viewLabel = new MenuItem
        {
            Text = "View"
        };
        viewLabel.Controller = new TestMenuItemController(viewLabel, contextMenuManager);
        
        var specialMenuItem = new MenuItem
        {
            Text = "Special",
            IsDisabled = true
        };

        var helpLabel = new MenuItem
        {
            Text = "Help"
        };
        helpLabel.Controller = new TestMenuItemController(helpLabel, contextMenuManager);
        
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
            },
            Children =
            {
                row
            }
        };
        
        var container = new Panel
        {
            BackgroundColor = 0x000000,
            Padding = new PaddingStyle
            {
                Bottom = 1,
            },
            Children =
            {
                background
            }
        };

        Add(container);
    }
}