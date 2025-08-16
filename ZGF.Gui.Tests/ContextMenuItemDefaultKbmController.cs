namespace ZGF.Gui.Tests;

public sealed class ContextMenuItemDefaultKbmController : IKeyboardMouseController
{
    private readonly ContextMenu _contextMenu;
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

    public void OnMouseEnter(ref MouseEnterEvent e)
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

            _openedContextMenu = _contextMenuManager.ShowContextMenu(subMenu, _contextMenu);
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

    public void OnMouseExit(ref MouseExitEvent e)
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

    public void OnMouseButtonStateChanged(ref MouseButtonEvent e)
    {
        if (e.State == InputState.Pressed)
        {
            Clicked?.Invoke();
            e.Consume();
        }
    }

    public void OnMouseWheelScrolled(ref MouseWheelScrolledEvent e)
    {
    }

    public void OnMouseMoved(ref MouseMoveEvent e)
    {
    }

    public void OnKeyboardKeyStateChanged(ref KeyboardKeyEvent e)
    {
    }

    public void OnFocusLost()
    {
    }

    public void OnFocusGained()
    {
    }

    public View View => _contextMenuItem;
}