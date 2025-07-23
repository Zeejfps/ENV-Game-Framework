namespace ZGF.Gui.Tests;

public abstract class BaseMenuItemController : IKeyboardMouseController
{
    protected MenuItem MenuItem { get; }
    protected readonly ContextMenuManager _contextMenuManager;
    
    private ContextMenu? _contextMenu;
    private IOpenedContextMenu? _openedContextMenu;

    protected BaseMenuItemController(MenuItem menuItem, ContextMenuManager contextMenuManager)
    {
        MenuItem = menuItem;
        _contextMenuManager = contextMenuManager;
    }
    
    public void OnEnabled(Context context)
    {
        this.RegisterController(context);
    }

    public void OnDisabled(Context context)
    {
        this.UnregisterController(context);
    }
    
    public void OnMouseEnter()
    {
        MenuItem.IsSelected = true;
        if (_openedContextMenu != null && _openedContextMenu.IsOpened)
        {
            _openedContextMenu.KeepOpen();
            return;
        }

        _contextMenu = new ContextMenu
        {
            AnchorPoint = MenuItem.Position.BottomLeft
        };
        _contextMenu.AddController(new ContextMenuDefaultKbmController(_contextMenu));
        BuildMenu(_contextMenu);
        
        _openedContextMenu = _contextMenuManager.ShowContextMenu(_contextMenu);
    }

    public void OnMouseExit()
    {
        MenuItem.IsSelected = false;
        if (_openedContextMenu != null && _openedContextMenu.IsOpened)
        {
            _openedContextMenu.Close();
        }
    }
    
    public Component Component => MenuItem;

    protected abstract void BuildMenu(ContextMenu contextMenu);
}