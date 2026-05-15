namespace ZGF.Gui.Tests;

public abstract class BaseMenuItemController : KeyboardMouseController
{
    protected MenuItem MenuItem { get; }
    protected ContextMenuManager? _contextMenuManager;

    private ContextMenu? _contextMenu;
    private IOpenedContextMenu? _openedContextMenu;
    private readonly List<(MultiChildView View, KeyboardMouseController Controller)> _menuRegistrations = new();

    protected BaseMenuItemController(MenuItem menuItem)
    {
        MenuItem = menuItem;
    }

    protected override void OnAttachedToContext(MultiChildView view, Context context)
    {
        _contextMenuManager = context.Get<ContextMenuManager>();
    }

    protected override void OnDetachedFromContext(MultiChildView view, Context context)
    {
        if (_openedContextMenu != null)
        {
            _openedContextMenu.Closed -= OnOpenedContextMenuClosed;
            _openedContextMenu = null;
        }
        UnregisterMenuControllers();
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
        BuildMenu(_contextMenu);

        _openedContextMenu = _contextMenuManager!.ShowContextMenu(_contextMenu);
        if (_openedContextMenu != null)
        {
            _openedContextMenu.Closed += OnOpenedContextMenuClosed;
            RegisterMenuController(_contextMenu, new ContextMenuKbmController(_openedContextMenu));
            MenuItem.IsSelected = true;
        }
    }

    protected void RegisterMenuController(MultiChildView view, KeyboardMouseController controller)
    {
        view.Behaviors.Add(controller);
        _menuRegistrations.Add((view, controller));
    }

    private void UnregisterMenuControllers()
    {
        foreach (var (view, controller) in _menuRegistrations)
        {
            view.Behaviors.Remove(controller);
        }
        _menuRegistrations.Clear();
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