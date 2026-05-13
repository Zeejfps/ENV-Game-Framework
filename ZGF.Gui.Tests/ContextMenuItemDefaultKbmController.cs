namespace ZGF.Gui.Tests;

public sealed class ContextMenuItemDefaultKbmController : KeyboardMouseController
{
    private readonly ContextMenuItem _contextMenuItem;
    private ContextMenuManager? _contextMenuManager;
    private IOpenedContextMenu? _openedContextMenu;
    private InputSystem? _subMenuInputSystem;
    private ContextMenu? _registeredSubMenu;

    public List<ContextMenuItemData> SubOptions { get; } = new();

    public Action? Clicked
    {
        get;
        set;
    }

    public ContextMenuItemDefaultKbmController(
        ContextMenuItem contextMenuItem,
        Action? clicked = null)
    {
        _contextMenuItem = contextMenuItem;
        Clicked = clicked;
    }

    public override void OnAttachedToContext(View view, Context context)
    {
        _contextMenuManager = context.Get<ContextMenuManager>();
        base.OnAttachedToContext(view, context);
    }

    public override void OnAttached()
    {
        base.OnAttached();
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
            _openedContextMenu = _contextMenuManager!.ShowContextMenu(subMenu, parentMenu);
            if (_openedContextMenu != null)
            {
                _openedContextMenu.Closed += OnOpenedContextMenuClosed;
                inputSystem?.RegisterController(subMenu, new ContextMenuKbmController(_openedContextMenu));
                _subMenuInputSystem = inputSystem;
                _registeredSubMenu = subMenu;
            }
        }

        _contextMenuItem.IsSelected = true;
        inputSystem?.RequestFocus(this);
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
        _contextMenuItem.Context?.Get<InputSystem>()!.Blur(this);
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
        _contextMenuItem.Context?.Get<InputSystem>()!.Blur(this);
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