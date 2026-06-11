using ZGF.Gui.Desktop;
using ZGF.Gui.Desktop.Components.ContextMenu;
using ZGF.Gui.Desktop.Controllers;
using ZGF.Gui.Desktop.Input;
using ZGF.Observable;

namespace ZGF.Gui.Sandbox;

public abstract class BaseMenuItemController : KeyboardMouseController, IDisposable
{
    protected View MenuItem { get; }
    private readonly State<bool> _isSelected;
    private readonly IContextMenuHost _contextMenuManager;
    private readonly IWindowCoordinates? _coordinates;

    private IOpenedContextMenu? _openedContextMenu;
    private InputSystem? _menuInputSystem;
    private ContextMenu? _registeredMenu;

    protected BaseMenuItemController(View menuItem, State<bool> isSelected, Context context)
    {
        MenuItem = menuItem;
        _isSelected = isSelected;
        _contextMenuManager = context.Get<IContextMenuHost>()!;
        _coordinates = context.Get<IWindowCoordinates>();
    }

    public void Dispose()
    {
        if (_openedContextMenu != null)
        {
            _openedContextMenu.Closed -= OnOpenedContextMenuClosed;
            _openedContextMenu = null;
        }
        UnregisterMenuController();
    }

    public override void OnMouseEnter(ref MouseEnterEvent e)
    {
        _openedContextMenu?.CancelCloseRequest();
    }

    // Click-to-open: the pointer arbiter clears main-window hover whenever a modal popup
    // is open, so a hover-opened menu would immediately receive a synthetic exit and
    // close itself. Opening on press matches the framework's modal design; the menu
    // closes on outside click (ContextMenuManager's main-window press preview).
    public override void OnMouseButtonStateChanged(ref MouseButtonEvent e)
    {
        if (e.State != InputState.Pressed || e.Button != MouseButton.Left)
            return;
        if (_openedContextMenu != null && _openedContextMenu.IsOpened)
            return;

        var screen = _coordinates != null
            ? _coordinates.ToScreenPoints(MenuItem.Position.BottomLeft)
            : default;
        _openedContextMenu = _contextMenuManager.ShowContextMenu(popupCtx =>
        {
            var contextMenu = new ContextMenu();
            BuildMenu(contextMenu, popupCtx);
            return contextMenu;
        }, screen);
        if (_openedContextMenu != null)
        {
            _openedContextMenu.Closed += OnOpenedContextMenuClosed;
            var menuInput = _openedContextMenu.Context.Get<InputSystem>();
            menuInput?.RegisterController(_openedContextMenu.Menu, new ContextMenuKbmController(_openedContextMenu));
            _menuInputSystem = menuInput;
            _registeredMenu = _openedContextMenu.Menu;
            _isSelected.Value = true;
            e.Consume();
        }
    }

    private void UnregisterMenuController()
    {
        if (_registeredMenu != null)
        {
            _menuInputSystem?.UnregisterController(_registeredMenu);
            _registeredMenu = null;
        }
        _menuInputSystem = null;
    }

    private void OnOpenedContextMenuClosed()
    {
        _isSelected.Value = false;
        if (_openedContextMenu != null)
        {
            _openedContextMenu.Closed -= OnOpenedContextMenuClosed;
            _openedContextMenu = null;
        }
        UnregisterMenuController();
    }

    protected abstract void BuildMenu(ContextMenu contextMenu, Context popupContext);
}
