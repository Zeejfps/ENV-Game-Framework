namespace ZGF.Gui.Tests;

public sealed class ContextMenuItemDefaultKbmController : KeyboardMouseController
{
    private readonly ContextMenuItem _contextMenuItem;
    private readonly ContextMenuManager _contextMenuManager;
    private IOpenedContextMenu? _openedContextMenu;

    public List<ContextMenuItemData> SubOptions { get; } = new();

    public Action? Clicked
    {
        get;
        set;
    }
    
    public ContextMenuItemDefaultKbmController(
        ContextMenuItem contextMenuItem,
        ContextMenuManager contextMenuManager,
        Action? clicked = null)
    {
        _contextMenuItem = contextMenuItem;
        _contextMenuManager = contextMenuManager;
        Clicked = clicked;
    }

    public override void OnEnabled(Context context)
    {
        base.OnEnabled(context);
        if (SubOptions.Count > 0)
        {
            _contextMenuItem.IsArrowVisible = true;
        }
    }
    

    public override void OnMouseEnter(ref MouseEnterEvent e)
    {
        if (_openedContextMenu != null && _openedContextMenu.IsOpened)
        {
            _openedContextMenu.CancelCloseRequest();
            return;
        }

        if (SubOptions.Count > 0)
        {
            var subMenu = new ContextMenu
            {
                AnchorPoint = _contextMenuItem.Position.TopRight
            };
            foreach (var subOption in SubOptions)
            {
                subMenu.Children.Add(new ContextMenuItem
                {
                    Text = subOption.Text
                });
            }

            var parentMenu = _contextMenuItem.GetParentOfType<ContextMenu>();
            _openedContextMenu = _contextMenuManager.ShowContextMenu(subMenu, parentMenu);
            if (_openedContextMenu != null)
            {
                _openedContextMenu.Closed += OnOpenedContextMenuClosed;
                subMenu.Controller = new ContextMenuKbmController(_openedContextMenu);
            }
        }
        
        _contextMenuItem.IsSelected = true;
        this.RequestFocus();
    }

    private void OnOpenedContextMenuClosed()
    {
        _contextMenuItem.IsSelected = false;
        if (_openedContextMenu != null)
        {
            _openedContextMenu.Closed -=  OnOpenedContextMenuClosed;
            _openedContextMenu = null;
        }
    }

    public override void OnMouseExit(ref MouseExitEvent e)
    {
        if (_openedContextMenu != null && _openedContextMenu.IsOpened)
        {
            _openedContextMenu.CloseRequest();
        }
        else
        {
            _contextMenuItem.IsSelected = false;       
        }
        this.Blur();
    }

    public override void OnMouseButtonStateChanged(ref MouseButtonEvent e)
    {
        if (e.State == InputState.Pressed)
        {
            Clicked?.Invoke();
            e.Consume();
        }
    }
    
    public override View View => _contextMenuItem;
}