using ZGF.Gui.Desktop.Controllers;
using ZGF.Gui.Desktop.Input;

namespace ZGF.Gui.Desktop.Components.ContextMenu;

public sealed class ContextMenuItemDefaultKbmController : KeyboardMouseController, IDisposable
{
    private readonly ContextMenuItem _contextMenuItem;
    private readonly IContextMenuHost _contextMenuManager;
    private IOpenedContextMenu? _openedContextMenu;
    private InputSystem? _subMenuInputSystem;
    private ContextMenu? _registeredSubMenu;
    private readonly Func<ContextMenu>? _subMenuFactory;

    public List<ContextMenuItemData> SubOptions { get; }

    public Action? Clicked { get; set; }

    public ContextMenuItemDefaultKbmController(
        ContextMenuItem contextMenuItem,
        Context context,
        Action? clicked = null,
        IEnumerable<ContextMenuItemData>? subOptions = null,
        Func<ContextMenu>? subMenuFactory = null)
    {
        _contextMenuItem = contextMenuItem;
        _contextMenuManager = context.Get<IContextMenuHost>()!;
        Clicked = clicked;
        _subMenuFactory = subMenuFactory;
        SubOptions = subOptions == null ? new List<ContextMenuItemData>() : new List<ContextMenuItemData>(subOptions);
        if (SubOptions.Count > 0 || subMenuFactory != null)
            _contextMenuItem.IsArrowVisible = true;
    }

    public void Dispose()
    {
        if (_openedContextMenu != null)
        {
            _openedContextMenu.Closed -= OnOpenedContextMenuClosed;
            _openedContextMenu = null;
        }
        if (_registeredSubMenu != null)
        {
            _subMenuInputSystem?.UnregisterController(_registeredSubMenu);
            _registeredSubMenu = null;
        }
        _subMenuInputSystem = null;
    }

    public override void OnMouseEnter(ref MouseEnterEvent e)
    {
        if (!_contextMenuItem.IsEnabled) return;
        if (_openedContextMenu != null && _openedContextMenu.IsOpened)
        {
            _openedContextMenu.CancelCloseRequest();
            return;
        }

        var subMenu = BuildSubMenu();
        if (subMenu != null)
        {
            var parentMenu = _contextMenuItem.GetParentOfType<ContextMenu>();
            // Resolve coordinates from this menu item's context (the parent
            // popup's context, which registers its own IWindowCoordinates).
            // Translating via the main window's coordinates would put the
            // submenu near the main window's origin instead of next to the
            // parent menu item.
            var coords = _contextMenuItem.Context?.Get<IWindowCoordinates>();
            var screenAnchor = coords != null
                ? coords.ToScreenPoints(_contextMenuItem.Position.TopRight)
                : default;
            _openedContextMenu = _contextMenuManager.ShowContextMenu(subMenu, screenAnchor, parentMenu);
            if (_openedContextMenu != null)
            {
                _openedContextMenu.Closed += OnOpenedContextMenuClosed;
                // Keep the submenu open while the pointer is over it. The controller
                // must live on the submenu's OWN popup input system — its context is
                // assigned when the popup adopts the submenu as its root — so it
                // receives the submenu's enter/exit events (the parent popup's input
                // system never sees them).
                var subInput = subMenu.Context?.Get<InputSystem>();
                subInput?.RegisterController(subMenu, new ContextMenuKbmController(_openedContextMenu));
                _subMenuInputSystem = subInput;
                _registeredSubMenu = subMenu;
            }
        }

        _contextMenuItem.IsSelected = true;
    }

    // A caller-supplied factory takes precedence (its items carry their own actions and
    // styling); otherwise fall back to the plain text-only SubOptions list.
    private ContextMenu? BuildSubMenu()
    {
        if (_subMenuFactory != null)
            return _subMenuFactory();
        if (SubOptions.Count == 0)
            return null;
        var subMenu = new ContextMenu();
        foreach (var subOption in SubOptions)
            subMenu.Children.Add(new ContextMenuItem { Text = subOption.Text });
        return subMenu;
    }

    private void OnOpenedContextMenuClosed()
    {
        _contextMenuItem.IsSelected = false;
        if (_openedContextMenu != null)
        {
            _openedContextMenu.Closed -=  OnOpenedContextMenuClosed;
            _openedContextMenu = null;
        }
        if (_registeredSubMenu != null)
        {
            _subMenuInputSystem?.UnregisterController(_registeredSubMenu);
            _registeredSubMenu = null;
        }
        _subMenuInputSystem = null;
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
    }

    public override void OnMouseButtonStateChanged(ref MouseButtonEvent e)
    {
        if (e.State == InputState.Pressed)
        {
            if (!_contextMenuItem.IsEnabled)
            {
                // Consume so the press doesn't fall through to dismiss the menu — the user
                // clicked an item (just a disabled one), not outside the menu.
                e.Consume();
                return;
            }
            Clicked?.Invoke();
            e.Consume();
        }
    }
}
