﻿namespace ZGF.Gui.Tests;

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
        if (_openedContextMenu != null)
        {
            _openedContextMenu.Closed -= OnOpenedContextMenuClosed;
            _openedContextMenu = null;
        }
        this.UnregisterController(context);
    }
    
    public void OnMouseEnter(ref MouseEnterEvent e)
    {
        if (_openedContextMenu != null && _openedContextMenu.IsOpened)
        {
            _openedContextMenu.CancelCloseRequest();
            return;
        }

        _contextMenu = new ContextMenu
        {
            AnchorPoint = MenuItem.Position.BottomLeft
        };
        BuildMenu(_contextMenu);
        
        _openedContextMenu = _contextMenuManager.ShowContextMenu(_contextMenu);
        if (_openedContextMenu != null)
        {
            _openedContextMenu.Closed += OnOpenedContextMenuClosed;
            _contextMenu.Controller = new ContextMenuKbmController(_openedContextMenu);
            MenuItem.IsSelected = true;
        }
    }

    private void OnOpenedContextMenuClosed()
    {
        MenuItem.IsSelected = false;
        if (_openedContextMenu != null)
        {
            _openedContextMenu.Closed -= OnOpenedContextMenuClosed;
            _openedContextMenu = null;
        }
    }

    public void OnMouseExit(ref MouseExitEvent e)
    {
        if (_openedContextMenu != null && _openedContextMenu.IsOpened)
        {
            _openedContextMenu.CloseRequest();
        }
    }

    public void OnMouseButtonStateChanged(ref MouseButtonEvent e)
    {
        
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

    public View View => MenuItem;

    protected abstract void BuildMenu(ContextMenu contextMenu);
}