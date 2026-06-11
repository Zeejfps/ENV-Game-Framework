using ZGF.Gui.Desktop.Controllers;
using ZGF.Gui.Desktop.Input;

namespace ZGF.Gui.Desktop.Components.ContextMenu;

public sealed class ContextMenuItemDefaultKbmController : KeyboardMouseController, IDisposable
{
    private readonly ContextMenuItem _contextMenuItem;
    private readonly IContextMenuHost _contextMenuManager;
    private readonly IWindowCoordinates? _coordinates;
    private IOpenedContextMenu? _openedContextMenu;
    private InputSystem? _subMenuInputSystem;
    private ContextMenu? _registeredSubMenu;
    private readonly Func<Context, ContextMenu>? _subMenuFactory;

    public List<ContextMenuItemData> SubOptions { get; }

    public Action? Clicked { get; set; }

    /// <summary>
    /// <paramref name="context"/> is the build context of the window the item is built for —
    /// for items inside a menu that is the popup's own context, so the captured coordinates
    /// translate from the popup the item actually lives in.
    /// </summary>
    public ContextMenuItemDefaultKbmController(
        ContextMenuItem contextMenuItem,
        Context context,
        Action? clicked = null,
        IEnumerable<ContextMenuItemData>? subOptions = null,
        Func<Context, ContextMenu>? subMenuFactory = null)
    {
        _contextMenuItem = contextMenuItem;
        _contextMenuManager = context.Get<IContextMenuHost>()!;
        _coordinates = context.Get<IWindowCoordinates>();
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

        var buildSubMenu = GetSubMenuBuilder();
        if (buildSubMenu != null)
        {
            var parentMenu = _contextMenuItem.GetParentOfType<ContextMenu>();
            var screenAnchor = _coordinates != null
                ? _coordinates.ToScreenPoints(_contextMenuItem.Position.TopRight)
                : default;
            _openedContextMenu = _contextMenuManager.ShowContextMenu(buildSubMenu, screenAnchor, parentMenu);
            if (_openedContextMenu != null)
            {
                _openedContextMenu.Closed += OnOpenedContextMenuClosed;
                // Keep the submenu open while the pointer is over it. The controller
                // must live on the submenu's OWN popup input system so it receives the
                // submenu's enter/exit events (the parent popup's input system never
                // sees them). The submenu was built against the popup's context, which
                // the opened handle exposes.
                var subInput = _openedContextMenu.Context.Get<InputSystem>();
                subInput?.RegisterController(_openedContextMenu.Menu, new ContextMenuKbmController(_openedContextMenu));
                _subMenuInputSystem = subInput;
                _registeredSubMenu = _openedContextMenu.Menu;
            }
        }

        _contextMenuItem.IsSelected = true;
    }

    // A caller-supplied factory takes precedence (its items carry their own actions and
    // styling); otherwise fall back to the plain text-only SubOptions list. Either way the
    // menu is built against the submenu popup's own context.
    private Func<Context, ContextMenu>? GetSubMenuBuilder()
    {
        if (_subMenuFactory != null)
            return _subMenuFactory;
        if (SubOptions.Count == 0)
            return null;
        var subOptions = SubOptions;
        return ctx =>
        {
            var subMenu = new ContextMenu();
            var subInput = ctx.Get<InputSystem>();
            foreach (var subOption in subOptions)
            {
                var subItem = new ContextMenuItem(ctx.Canvas) { Text = subOption.Text };
                if (subInput != null)
                    subItem.UseController(subInput, () => new ContextMenuItemDefaultKbmController(subItem, ctx));
                subMenu.Children.Add(subItem);
            }
            return subMenu;
        };
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
            // A leaf item click completes the interaction: close the whole menu chain.
            // Submenu parents stay open — their click is not a command. Deferred close
            // (RequestCloseAll) is required mid-dispatch; see ContextMenuManager.
            if (SubOptions.Count == 0 && _subMenuFactory == null)
                _contextMenuManager.RequestCloseAll();
            e.Consume();
        }
    }
}
