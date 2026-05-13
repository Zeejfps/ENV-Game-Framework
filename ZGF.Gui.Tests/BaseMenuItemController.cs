namespace ZGF.Gui.Tests;

public abstract class BaseMenuItemController : KeyboardMouseController
{
    protected MenuItem MenuItem { get; }
    protected ContextMenuManager? _contextMenuManager;

    private ContextMenu? _contextMenu;
    private IOpenedContextMenu? _openedContextMenu;
    private InputSystem? _menuInputSystem;
    private readonly List<View> _menuRegistrations = new();

    protected BaseMenuItemController(MenuItem menuItem)
    {
        MenuItem = menuItem;
    }

    public override void OnAttachedToContext(View view, Context context)
    {
        _contextMenuManager = context.Get<ContextMenuManager>();
        base.OnAttachedToContext(view, context);
    }

    public override void OnDetached()
    {
        if (_openedContextMenu != null)
        {
            _openedContextMenu.Closed -= OnOpenedContextMenuClosed;
            _openedContextMenu = null;
        }
        UnregisterMenuControllers();
        base.OnDetached();
    }

    public override void OnMouseEnter(ref MouseEnterEvent e)
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
        _menuInputSystem = MenuItem.Context?.Get<InputSystem>();
        BuildMenu(_contextMenu);

        _openedContextMenu = _contextMenuManager!.ShowContextMenu(_contextMenu);
        if (_openedContextMenu != null)
        {
            _openedContextMenu.Closed += OnOpenedContextMenuClosed;
            RegisterMenuController(_contextMenu, new ContextMenuKbmController(_openedContextMenu));
            MenuItem.IsSelected = true;
        }
    }

    protected void RegisterMenuController(View view, IKeyboardMouseController controller)
    {
        _menuInputSystem?.RegisterController(view, controller);
        _menuRegistrations.Add(view);
    }

    private void UnregisterMenuControllers()
    {
        if (_menuInputSystem != null)
        {
            foreach (var view in _menuRegistrations)
            {
                _menuInputSystem.UnregisterController(view);
            }
        }
        _menuRegistrations.Clear();
        _menuInputSystem = null;
    }

    private void OnOpenedContextMenuClosed()
    {
        MenuItem.IsSelected = false;
        if (_openedContextMenu != null)
        {
            _openedContextMenu.Closed -= OnOpenedContextMenuClosed;
            _openedContextMenu = null;
        }
        UnregisterMenuControllers();
    }

    public override void OnMouseExit(ref MouseExitEvent e)
    {
        if (_openedContextMenu != null && _openedContextMenu.IsOpened)
        {
            _openedContextMenu.CloseRequest();
        }
    }

    protected abstract void BuildMenu(ContextMenu contextMenu);
}