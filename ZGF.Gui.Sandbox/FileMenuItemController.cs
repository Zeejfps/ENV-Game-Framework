using ZGF.Gui.Desktop.Components.ContextMenu;
using ZGF.Gui.Desktop.Controllers;
using ZGF.Gui.Desktop.Input;
using ZGF.Observable;

namespace ZGF.Gui.Sandbox;

public sealed class FileMenuItemController : BaseMenuItemController
{
    private readonly App _app;

    public FileMenuItemController(View menuItem, State<bool> isSelected, App app, Context context) : base(menuItem, isSelected, context)
    {
        _app = app;
    }

    protected override void BuildMenu(ContextMenu contextMenu, Context popupContext)
    {
        var openModelItem = new ContextMenuItem(popupContext.Canvas)
        {
            Text = "Open Model",
        };
        contextMenu.Children.Add(openModelItem);

        var exitItem = new ContextMenuItem(popupContext.Canvas)
        {
            Text = "Exit",
        };
        contextMenu.Children.Add(exitItem);
        exitItem.UseController(popupContext.Require<InputSystem>(), () => new ContextMenuItemDefaultKbmController(exitItem, popupContext, () =>
        {
            _app.Exit();
        }));
    }
}
