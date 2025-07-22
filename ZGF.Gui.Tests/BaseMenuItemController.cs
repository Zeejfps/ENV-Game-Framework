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
    
    public void OnMouseEnter()
    {
        MenuItem.IsHovered = true;
        _contextMenu = _contextMenuManager
            .ShowContextMenu(MenuItem.Position.BottomLeft);
        _contextMenu.AddController(new ContextMenuDefaultKbmController(MenuItem, _contextMenu));
        BuildMenu(_contextMenu);
    }

    public void OnMouseExit()
    {
        MenuItem.IsHovered = false;
        SubmitMenuCloseRequest();
    }

    // public void OnFocusLost()
    // {
    //     MenuItem.IsHovered = false;
    //     SubmitMenuCloseRequest();
    // }

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
    
    public Component Component => MenuItem;

    protected abstract void BuildMenu(ContextMenu contextMenu);
}