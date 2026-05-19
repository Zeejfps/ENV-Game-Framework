namespace ZGF.Gui.Tests;

public sealed class ContextMenuItemDefaultKbmController : KeyboardMouseController, IDisposable
{
    private readonly ContextMenuItem _contextMenuItem;
    private readonly ContextMenuManager _contextMenuManager;
    private IOpenedContextMenu? _openedContextMenu;
    private InputSystem? _subMenuInputSystem;
    private ContextMenu? _registeredSubMenu;

    public List<ContextMenuItemData> SubOptions { get; }

    public Action? Clicked { get; set; }

    public ContextMenuItemDefaultKbmController(
        ContextMenuItem contextMenuItem,
        Context context,
        Action? clicked = null,
        IEnumerable<ContextMenuItemData>? subOptions = null)
    {
        _contextMenuItem = contextMenuItem;
        _contextMenuManager = context.Get<ContextMenuManager>()!;
        Clicked = clicked;
        SubOptions = subOptions == null ? new List<ContextMenuItemData>() : new List<ContextMenuItemData>(subOptions);
        if (SubOptions.Count > 0)
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
        if (_openedContextMenu != null && _openedContextMenu.IsOpened)
        {
            _openedContextMenu.CancelCloseRequest();
            return;
        }

        var inputSystem = _contextMenuItem.Context?.Get<InputSystem>();

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
                inputSystem?.RegisterController(subMenu, new ContextMenuKbmController(_openedContextMenu));
                _subMenuInputSystem = inputSystem;
                _registeredSubMenu = subMenu;
            }
        }

        _contextMenuItem.IsSelected = true;
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
            Clicked?.Invoke();
            e.Consume();
        }
    }
}
