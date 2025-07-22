namespace ZGF.Gui.Tests;

public sealed class SubContextMenuKbmController : IKeyboardMouseController
{
    private readonly ContextMenuItem _contextMenuItem;
    private readonly ContextMenu _parentContextMenu;
    private readonly ContextMenu _contextMenu;
    private readonly ContextMenuManager _contextMenuManager;

    public SubContextMenuKbmController(
        ContextMenuItem contextMenuItem,
        ContextMenu contextMenu,
        ContextMenu parentContextMenu,
        ContextMenuManager contextMenuManager)
    {
        _contextMenuItem = contextMenuItem;
        _parentContextMenu = parentContextMenu;
        _contextMenuManager = contextMenuManager;
        _contextMenu = contextMenu;
    }

    public void OnEnabled(Context context)
    {
        Console.WriteLine("OnEnabled");
        this.RegisterController(context);
    }

    public void OnDisabled(Context context)
    {
        Console.WriteLine("OnDisabled");
        this.UnregisterController(context);
    }

    public void OnMouseEnter()
    {
        Console.WriteLine("OnMouseEnter");
        _contextMenuItem.IsSelected = true;
        _contextMenuManager.SetKeepOpen(_parentContextMenu);
        _contextMenuManager.SetKeepOpen(_contextMenu);
    }

    public void OnMouseExit()
    { 
        Console.WriteLine("OnMouseExit");
        _contextMenuItem.IsSelected = false;
        _contextMenuManager.HideContextMenu(_parentContextMenu);
        _contextMenuManager.HideContextMenu(_contextMenu);
    }

    public Component Component => _contextMenu;
}

public sealed class ContextMenuItemDefaultKbmController : IKeyboardMouseController
{
    private readonly ContextMenu _contextMenu;
    private readonly ContextMenuItem _contextMenuItem;
    private readonly ContextMenuManager _contextMenuManager;
    private ContextMenu? _subMenu;

    public List<ContextMenuItemData> SubOptions { get; } = new();

    public Action? Clicked
    {
        get;
        set;
    }
    
    public ContextMenuItemDefaultKbmController(
        ContextMenu contextMenu,
        ContextMenuItem contextMenuItem,
        ContextMenuManager contextMenuManager,
        Action? clicked = null)
    {
        _contextMenuItem = contextMenuItem;
        _contextMenu = contextMenu;
        _contextMenuManager = contextMenuManager;
        Clicked = clicked;
    }

    public void OnEnabled(Context context)
    {
        context.InputSystem.AddInteractable(this);
        if (SubOptions.Count > 0)
        {
            _contextMenuItem.IsArrowVisible = true;
        }
    }

    public void OnDisabled(Context context)
    {
        context.InputSystem.RemoveInteractable(this);
    }

    public void OnMouseEnter()
    {
        _contextMenuItem.IsSelected = true;
        if (SubOptions.Count > 0)
        {
            _subMenu = _contextMenuManager.ShowContextMenu(_contextMenuItem.Position.TopRight, _contextMenu);
            _subMenu.AddController(new SubContextMenuKbmController(_contextMenuItem, _contextMenu, _subMenu, _contextMenuManager));
            foreach (var subOption in SubOptions)
            {
                _subMenu.AddItem(new ContextMenuItem
                {
                    Text = subOption.Text
                });
            }
        }

        this.RequestFocus();
    }

    public void OnMouseExit()
    {
        _contextMenuItem.IsSelected = false;
        if (_subMenu != null)
        {
            _contextMenuManager?.HideContextMenu(_subMenu);
        }
        this.Blur();
    }

    public bool OnMouseButtonStateChanged(in MouseButtonEvent e)
    {
        if (e.State == InputState.Pressed)
        {
            Clicked?.Invoke();
            return true;
        }
        return false;
    }

    public Component Component => _contextMenuItem;
}