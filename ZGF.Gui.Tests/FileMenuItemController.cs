namespace ZGF.Gui.Tests;

public sealed class FileMenuItemController : BaseMenuItemController 
{
    private readonly App _app;

    public FileMenuItemController(MenuItem menuItem, ContextMenuManager contextMenuManager, App app) : base(menuItem, contextMenuManager)
    {
        _app = app;
    }

    protected override void BuildMenu(ContextMenu contextMenu)
    {
        var openModelItem = new ContextMenuItem
        {
            Text = "Open Model",
        };
        contextMenu.AddItem(openModelItem);

        var exitItem = new ContextMenuItem
        {
            Text = "Exit",
        };
        exitItem.Controller = new ContextMenuItemDefaultKbmController(contextMenu, exitItem, _contextMenuManager, () =>
        {
            _app.Exit();
        });
        contextMenu.AddItem(exitItem);
    }
}