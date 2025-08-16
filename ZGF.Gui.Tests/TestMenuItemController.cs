namespace ZGF.Gui.Tests;

public sealed class TestMenuItemController : BaseMenuItemController
{
    public TestMenuItemController(MenuItem menuItem, ContextMenuManager contextMenuManager) : base(menuItem,
        contextMenuManager)
    {
        
    }

    protected override void BuildMenu(ContextMenu contextMenu)
    {
        contextMenu.Children.Add(new ContextMenuItem
        {
            Text = "Option 1"
        });
        contextMenu.Children.Add(new ContextMenuItem
        {
            Text = "Option 2"
        });

        var option3Menu = new ContextMenuItem
        {
            Text = "Option 3"
        };
        option3Menu.Controller = new ContextMenuItemDefaultKbmController(contextMenu, option3Menu, _contextMenuManager)
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
        };
        
        contextMenu.Children.Add(option3Menu);
        contextMenu.Children.Add(new ContextMenuItem
        {
            Text = "Option 4"
        });
    }
}