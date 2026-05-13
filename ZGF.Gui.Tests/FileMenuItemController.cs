namespace ZGF.Gui.Tests;

public sealed class FileMenuItemController : BaseMenuItemController 
{
    private readonly App _app;

    public FileMenuItemController(MenuItem menuItem, App app) : base(menuItem)
    {
        _app = app;
    }

    protected override void BuildMenu(ContextMenu contextMenu)
    {
        var openModelItem = new ContextMenuItem
        {
            Text = "Open Model",
        };
        contextMenu.Children.Add(openModelItem);

        var exitItem = new ContextMenuItem
        {
            Text = "Exit",
        };
        contextMenu.Children.Add(exitItem);
        RegisterMenuController(exitItem, new ContextMenuItemDefaultKbmController(exitItem, () =>
        {
            _app.Exit();
        }));
    }
}