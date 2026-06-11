using ZGF.Gui.Desktop.Components.ContextMenu;
using ZGF.Gui.Desktop.Controllers;
using ZGF.Gui.Desktop.Input;

namespace ZGF.Gui.Sandbox;

public sealed class TestMenuItemController : BaseMenuItemController
{
    public TestMenuItemController(MenuItem menuItem, Context context) : base(menuItem, context)
    {
    }

    protected override void BuildMenu(ContextMenu contextMenu, Context popupContext)
    {
        contextMenu.Children.Add(new ContextMenuItem(popupContext.Canvas)
        {
            Text = "Option 1",
        });
        contextMenu.Children.Add(new ContextMenuItem(popupContext.Canvas)
        {
            Text = "Option 2",
        });

        var option3Menu = new ContextMenuItem(popupContext.Canvas)
        {
            Text = "Option 3",
        };
        contextMenu.Children.Add(option3Menu);
        option3Menu.UseController(popupContext.Require<InputSystem>(), () => new ContextMenuItemDefaultKbmController(
            option3Menu,
            popupContext,
            subOptions: new[]
            {
                new ContextMenuItemData { Text = "Test1" },
                new ContextMenuItemData { Text = "Test2" },
                new ContextMenuItemData { Text = "Test3" },
            }));
        contextMenu.Children.Add(new ContextMenuItem(popupContext.Canvas)
        {
            SelectedBackgroundColor = 0xFFF0F0F0,
            Text = "Option 4",
        });
    }
}
