namespace ZGF.Gui.Tests;

public abstract class BaseMenuItemController : IKeyboardMouseController
{
    protected MenuItem MenuItem { get; }
    protected readonly ContextMenuManager _contextMenuManager;
    
    private ContextMenu _contextMenu;
    private bool _isBuilt;
    
    protected BaseMenuItemController(MenuItem menuItem, ContextMenuManager contextMenuManager)
    {
        MenuItem = menuItem;
        _contextMenuManager = contextMenuManager;
        
        _contextMenu = new ContextMenu();
        _contextMenu.AddController(new ContextMenuDefaultKbmController(_contextMenu));
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
        if (!_isBuilt)
        {
            BuildMenu(_contextMenu);
            _isBuilt = true;
        }
        
        _contextMenu.AnchorPoint = MenuItem.Position.BottomLeft;
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