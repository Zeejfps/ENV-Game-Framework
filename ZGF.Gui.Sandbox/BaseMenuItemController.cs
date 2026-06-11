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
        if (_openedContextMenu != null && _openedContextMenu.IsOpened)
        {
            _openedContextMenu.CancelCloseRequest();
            return;
        }

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

    public override void OnMouseExit(ref MouseExitEvent e)
    {
        if (_openedContextMenu != null && _openedContextMenu.IsOpened)
        {
            _openedContextMenu.CloseRequest();
        }
    }

    protected abstract void BuildMenu(ContextMenu contextMenu, Context popupContext);
}
