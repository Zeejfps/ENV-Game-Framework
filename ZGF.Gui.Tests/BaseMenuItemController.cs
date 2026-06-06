using ZGF.Gui.Desktop;

namespace ZGF.Gui.Tests;

public abstract class BaseMenuItemController : KeyboardMouseController, IDisposable
{
    protected MenuItem MenuItem { get; }
    protected Context Context { get; }
    private readonly IContextMenuHost _contextMenuManager;
    private readonly InputSystem _inputSystem;
    private readonly IWindowCoordinates? _coordinates;

    private ContextMenu? _contextMenu;
    private IOpenedContextMenu? _openedContextMenu;
    private readonly List<MultiChildView> _menuRegistrations = new();

    protected BaseMenuItemController(MenuItem menuItem, Context context)
    {
        MenuItem = menuItem;
        Context = context;
        _contextMenuManager = context.Get<IContextMenuHost>()!;
        _inputSystem = context.Get<InputSystem>()!;
        _coordinates = context.Get<IWindowCoordinates>();
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

        var screen = _coordinates != null
            ? _coordinates.ToScreenPoints(MenuItem.Position.BottomLeft)
            : default;
        _openedContextMenu = _contextMenuManager.ShowContextMenu(_contextMenu, screen);
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
