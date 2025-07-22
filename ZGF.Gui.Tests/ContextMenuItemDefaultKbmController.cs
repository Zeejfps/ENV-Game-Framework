namespace ZGF.Gui.Tests;

public sealed class ContextMenuItemDefaultKbmController : IKeyboardMouseController
{
    private readonly ContextMenu _contextMenu;
    private readonly ContextMenuItem _contextMenuItem;
    private ContextMenuManager? _contextMenuManager;
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
        Action? clicked = null)
    {
        _contextMenuItem = contextMenuItem;
        _contextMenu = contextMenu;
        Clicked = clicked;
    }

    public void OnEnabled(Context context)
    {
        context.InputSystem.AddInteractable(this);
        _contextMenuManager = context.Get<ContextMenuManager>();
        if (SubOptions.Count > 0)
        {
            _contextMenuItem.IsArrowVisible = true;
        }
    }

    public void OnDisabled(Context context)
    {
        context.InputSystem.RemoveInteractable(this);
        _contextMenuManager = null;
    }

    public void OnMouseEnter()
    {
        _contextMenuItem.BackgroundColor = 0x9C9CCE;
        if (SubOptions.Count > 0)
        {
            _subMenu = _contextMenuManager?.ShowContextMenu(_contextMenuItem.Position.TopRight, _contextMenu);
            if (_subMenu == null)
                return;
            
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
        _contextMenuItem.BackgroundColor = 0xDEDEDE;
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