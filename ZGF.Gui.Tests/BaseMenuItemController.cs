namespace ZGF.Gui.Tests;

public abstract class BaseMenuItemController : IKeyboardMouseController
{
    protected MenuItem MenuItem { get; }
    protected readonly ContextMenuManager _contextMenuManager;
    
    private ContextMenu? _contextMenu;

    protected BaseMenuItemController(MenuItem menuItem, ContextMenuManager contextMenuManager)
    {
        MenuItem = menuItem;
        _contextMenuManager = contextMenuManager;
    }
    
    public void OnEnabled(Context context)
    {
        this.RegisterController(context);
        
        _contextMenu = new ContextMenu(MenuItem.Position.BottomLeft);
        _contextMenu.AddController(new ContextMenuDefaultKbmController(_contextMenu));
        BuildMenu(_contextMenu);
    }

    public void OnDisabled(Context context)
    {
        this.UnregisterController(context);
    }
    
    public void OnMouseEnter()
    {
        MenuItem.IsSelected = true;
        if (_contextMenu != null)
        {
            _contextMenuManager.ShowContextMenu(_contextMenu);
        }
    }

    public void OnMouseExit()
    {
        MenuItem.IsSelected = false;
        if (_contextMenu != null)
        {
            _contextMenuManager.HideContextMenu(_contextMenu);
        }
    }
    
    public Component Component => MenuItem;

    protected abstract void BuildMenu(ContextMenu contextMenu);
}