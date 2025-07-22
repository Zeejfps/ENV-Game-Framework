namespace ZGF.Gui.Tests;

public sealed class FileMenuItemController : IKeyboardMouseController
{
    public Component Component => _menuItem;
    
    private readonly App _app;
    private readonly MenuItem _menuItem;
    private readonly ContextMenuManager _contextMenuManager;

    private ContextMenu? _contextMenu;

    public FileMenuItemController(MenuItem menuItem, ContextMenuManager contextMenuManager, App app)
    {
        _menuItem = menuItem;
        _contextMenuManager = contextMenuManager;
        _app = app;
    }
    
    public void OnMouseEnter()
    {
        Console.WriteLine("OnMouseEnter");

        _menuItem.IsHovered = true;
        _contextMenu = _contextMenuManager
            .ShowContextMenu(_menuItem.Position.BottomLeft);
        _contextMenu.AddController(new ContextMenuDefaultKbmController(_menuItem, _contextMenu));
        
        var openModelItem = new ContextMenuItem
        {
            Text = "Open Model",
        };
        _contextMenu.AddItem(openModelItem);

        var exitItem = new ContextMenuItem
        {
            Text = "Exit",
        };
        exitItem.AddController(new ContextMenuItemDefaultKbmController(_contextMenu, exitItem, () =>
        {
            _app.Exit();
        }));
        _contextMenu.AddItem(exitItem);
    }

    public void OnMouseExit()
    {
        _menuItem.IsHovered = false;
        SubmitMenuCloseRequest();
    }

    public void OnFocusLost()
    {
        _menuItem.IsHovered = false;
        SubmitMenuCloseRequest();
    }

    private void SubmitMenuCloseRequest()
    {
        if (_contextMenu != null)
        {
            _contextMenuManager.HideContextMenu(_contextMenu);
            _contextMenu = null;
        }
    }

    public void OnEnabled(Context context)
    {
        this.RegisterController(context);
    }

    public void OnDisabled(Context context)
    {
        this.UnregisterController(context);
    }
}