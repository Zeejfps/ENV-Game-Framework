using ZGF.Gui.Desktop.Components.ContextMenu;
using ZGF.Gui.Desktop.Controllers;
using ZGF.Gui.Desktop.Input;
using ZGF.Observable;

namespace ZGF.Gui.Sandbox;

public sealed class TestMenuItemController : BaseMenuItemController
{
    public TestMenuItemController(View menuItem, State<bool> isSelected, Context context) : base(menuItem, isSelected, context)
    {
    }

    protected override void BuildMenu(ContextMenu contextMenu, Context popupContext)
    {
        AddPlainItem(contextMenu, popupContext, "Option 1");
        AddPlainItem(contextMenu, popupContext, "Option 2");

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
        var option4 = new ContextMenuItem(popupContext.Canvas)
        {
            SelectedBackgroundColor = 0xFFF0F0F0,
            Text = "Option 4",
        };
        option4.UseController(popupContext.Require<InputSystem>(), () => new ContextMenuItemDefaultKbmController(option4, popupContext));
        contextMenu.Children.Add(option4);
    }

    private static void AddPlainItem(ContextMenu contextMenu, Context popupContext, string text)
    {
        var item = new ContextMenuItem(popupContext.Canvas) { Text = text };
        item.UseController(popupContext.Require<InputSystem>(), () => new ContextMenuItemDefaultKbmController(item, popupContext));
        contextMenu.Children.Add(item);
    }
}
