namespace ZGF.Gui.Tests;

public sealed class TestMenuItemController : BaseMenuItemController
{
    public TestMenuItemController(MenuItem menuItem, ContextMenuManager contextMenuManager, string text) : base(menuItem, contextMenuManager)
    {
        menuItem.Text = text;
    }

    protected override void BuildMenu(ContextMenu contextMenu)
    {
        contextMenu.AddItem(new ContextMenuItem
        {
            Text = "Option 1"
        });
        contextMenu.AddItem(new ContextMenuItem
        {
            Text = "Option 2"
        });

        var option3Menu = new ContextMenuItem
        {
            Text = "Option 3"
        };
        option3Menu.AddController(new ContextMenuItemDefaultKbmController(contextMenu, option3Menu, _contextMenuManager)
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
        
        contextMenu.AddItem(option3Menu);
        contextMenu.AddItem(new ContextMenuItem
        {
            Text = "Option 4"
        });
    }
}