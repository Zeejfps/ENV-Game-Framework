namespace ZGF.Gui.Tests;

public abstract class BaseMenuItemController : KeyboardMouseController, IDisposable
{
    protected MenuItem MenuItem { get; }
    protected Context Context { get; }
    private readonly ContextMenuManager _contextMenuManager;
    private readonly InputSystem _inputSystem;

    private ContextMenu? _contextMenu;
    private IOpenedContextMenu? _openedContextMenu;
    private readonly List<MultiChildView> _menuRegistrations = new();

    protected BaseMenuItemController(MenuItem menuItem, Context context)
    {
        MenuItem = menuItem;
        Context = context;
        _contextMenuManager = context.Get<ContextMenuManager>()!;
        _inputSystem = context.Get<InputSystem>()!;
    }

    public void Dispose()
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

        _contextMenu = new ContextMenu();
        BuildMenu(_contextMenu);

        _openedContextMenu = _contextMenuManager.ShowContextMenu(_contextMenu, MenuItem.Position.BottomLeft);
        if (_openedContextMenu != null)
        {
            _openedContextMenu.Closed += OnOpenedContextMenuClosed;
            RegisterMenuController(_contextMenu, new ContextMenuKbmController(_openedContextMenu));
            MenuItem.IsSelected = true;
        }
    }

    protected void RegisterMenuController(MultiChildView view, IKeyboardMouseController controller)
    {
        _inputSystem.RegisterController(view, controller);
        _menuRegistrations.Add(view);
    }

    private void UnregisterMenuControllers()
    {
        foreach (var view in _menuRegistrations)
        {
            _inputSystem.UnregisterController(view);
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
