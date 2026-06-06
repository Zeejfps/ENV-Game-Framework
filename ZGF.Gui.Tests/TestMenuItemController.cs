using ZGF.Gui.Desktop.Components.ContextMenu;

namespace ZGF.Gui.Tests;

public sealed class TestMenuItemController : BaseMenuItemController
{
    public TestMenuItemController(MenuItem menuItem, Context context) : base(menuItem, context)
    {
    }

    protected override void BuildMenu(ContextMenu contextMenu)
    {
        contextMenu.Children.Add(new ContextMenuItem
        {
            Text = "Option 1",
        });
        contextMenu.Children.Add(new ContextMenuItem
        {
            Text = "Option 2",
        });

        var option3Menu = new ContextMenuItem
        {
            Text = "Option 3",
        };
        contextMenu.Children.Add(option3Menu);
        RegisterMenuController(option3Menu, new ContextMenuItemDefaultKbmController(
            option3Menu,
            Context,
            subOptions: new[]
            {
                new ContextMenuItemData { Text = "Test1" },
                new ContextMenuItemData { Text = "Test2" },
                new ContextMenuItemData { Text = "Test3" },
            }));
        contextMenu.Children.Add(new ContextMenuItem
        {
            SelectedBackgroundColor = 0xFFF0F0F0,
            Text = "Option 4",
        });
    }
}
