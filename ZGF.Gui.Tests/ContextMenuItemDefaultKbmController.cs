namespace ZGF.Gui.Tests;

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
            _subMenu = new ContextMenu(_contextMenuItem.Position.TopRight);
            _subMenu.AddController(new ContextMenuDefaultKbmController(_contextMenu));
            foreach (var subOption in SubOptions)
            {
                _subMenu.AddItem(new ContextMenuItem
                {
                    Text = subOption.Text
                });
            }
            _contextMenuManager.ShowContextMenu(_subMenu, _contextMenu);
        }

        this.RequestFocus();
    }

    public void OnMouseExit()
    {
        _contextMenuItem.IsSelected = false;
        if (_subMenu != null)
        {
            _contextMenuManager.HideContextMenu(_subMenu);
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