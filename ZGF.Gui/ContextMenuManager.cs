using GLFW;
using ZGF.Geometry;

namespace ZGF.Gui.Tests;

public interface IOpenedContextMenu
{
    event Action Closed;
    bool IsOpened { get; }
    void CancelCloseRequest();
    void CloseRequest();
}

sealed class OpenedContextMenu : IOpenedContextMenu
{
    public event Action? Closed;

    public bool IsOpened { get; private set; } = true;
    public required ContextMenu ContextMenu { get; init; }
    public required IPopupWindow Popup { get; init; }
    public OpenedContextMenu? Parent { get; set; }
    public OpenedContextMenu? Child { get; set; }
    public long CloseTimestamp { get; set; }

    public bool IsCloseRequested { get; private set; }

    public void CancelCloseRequest()
    {
        if (!IsOpened) return;
        IsCloseRequested = false;
    }

    public void CloseRequest() => IsCloseRequested = true;

    public void Close()
    {
        IsOpened = false;
        Closed?.Invoke();
    }
}

public sealed class ContextMenuManager
{
    private readonly IPopupWindowFactory _popupFactory;
    private readonly IWindowCoordinates _coordinates;
    private readonly InputSystem? _mainInputSystem;
    private readonly MainWindowOutsideClickController _outsideClickController;
    private bool _outsideClickActive;

    private readonly HashSet<OpenedContextMenu> _closingMenus = new();
    private readonly Dictionary<ContextMenu, OpenedContextMenu> _openedMenus = new();

    public ContextMenuManager(
        IPopupWindowFactory popupFactory,
        IWindowCoordinates coordinates,
        InputSystem? mainInputSystem = null)
    {
        _popupFactory = popupFactory;
        _coordinates = coordinates;
        _mainInputSystem = mainInputSystem;
        _outsideClickController = new MainWindowOutsideClickController(this);
    }

    public IOpenedContextMenu? ShowContextMenu(ContextMenu menu, PointF canvasAnchor, ContextMenu? parentMenu = null)
    {
        if (_openedMenus.ContainsKey(menu))
            throw new System.Exception("Menu already opened");

        var width = (int)MathF.Ceiling(menu.MeasureWidth());
        var height = (int)MathF.Ceiling(menu.MeasureHeight(width));
        var screenAnchor = _coordinates.ToScreenPoints(canvasAnchor);

        var preferred = new RectI(
            X: screenAnchor.X,
            Y: screenAnchor.Y,
            Width: width,
            Height: height);

        var popup = _popupFactory.Acquire(new PopupRequest
        {
            Root = menu,
            PreferredScreenRect = preferred,
            FlippedScreenRect = null,
            MousePassThrough = false,
        });

        var opened = new OpenedContextMenu { ContextMenu = menu, Popup = popup };
        if (parentMenu != null)
        {
            if (!_openedMenus.TryGetValue(parentMenu, out var openedParent))
            {
                _popupFactory.Release(popup);
                return null;
            }
            openedParent.Child = opened;
            opened.Parent = openedParent;
        }

        _openedMenus[menu] = opened;
        popup.OutsideClick += HandleOutsideClick;
        EnsureMainOutsideClickHandler();
        return opened;
    }

    public void Update()
    {
        var now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        foreach (var menu in _closingMenus.ToList())
        {
            if (now - menu.CloseTimestamp > 100)
            {
                _popupFactory.Release(menu.Popup);
                _closingMenus.Remove(menu);
                _openedMenus.Remove(menu.ContextMenu);
                menu.Close();
            }
        }

        foreach (var opened in _openedMenus.Values.ToList())
        {
            if (opened.IsCloseRequested && opened.Child == null)
            {
                opened.CloseTimestamp = now;
                _closingMenus.Add(opened);
                _openedMenus.Remove(opened.ContextMenu);

                var parent = opened.Parent;
                if (parent != null) parent.Child = null;
            }
        }

        ReleaseMainOutsideClickHandlerIfEmpty();
    }

    public void RequestCloseMenu(ContextMenu menu)
    {
        if (_openedMenus.TryGetValue(menu, out var opened))
            opened.CloseRequest();
    }

    public void CloseAllImmediately()
    {
        foreach (var menu in _closingMenus.ToList())
        {
            _popupFactory.Release(menu.Popup);
            menu.Close();
        }
        _closingMenus.Clear();

        foreach (var opened in _openedMenus.Values.ToList())
        {
            _popupFactory.Release(opened.Popup);
            var parent = opened.Parent;
            if (parent != null) parent.Child = null;
            opened.Close();
        }
        _openedMenus.Clear();
        ReleaseMainOutsideClickHandlerIfEmpty();
    }

    private void EnsureMainOutsideClickHandler()
    {
        if (_outsideClickActive) return;
        if (_mainInputSystem == null) return;
        _mainInputSystem.StealFocus(_outsideClickController);
        _outsideClickActive = true;
    }

    private void ReleaseMainOutsideClickHandlerIfEmpty()
    {
        if (!_outsideClickActive) return;
        if (_openedMenus.Count > 0) return;
        if (_closingMenus.Count > 0) return;
        _mainInputSystem?.Blur(_outsideClickController);
        _outsideClickActive = false;
    }

    internal void HandleOutsideClick(PointI screenPoint)
    {
        foreach (var opened in _openedMenus.Values)
        {
            var window = (Window)opened.Popup.Window.WindowHandle;
            Glfw.GetWindowPosition(window, out var x, out var y);
            Glfw.GetWindowSize(window, out var w, out var h);
            if (screenPoint.X >= x && screenPoint.X < x + w &&
                screenPoint.Y >= y && screenPoint.Y < y + h)
                return;
        }
        CloseAllImmediately();
    }

    internal IWindowCoordinates Coordinates => _coordinates;
    internal IReadOnlyCollection<OpenedContextMenu> OpenedMenus => _openedMenus.Values;

    private sealed class MainWindowOutsideClickController : KeyboardMouseController
    {
        private readonly ContextMenuManager _manager;
        public MainWindowOutsideClickController(ContextMenuManager manager) { _manager = manager; }

        public override void OnMouseButtonStateChanged(ref MouseButtonEvent e)
        {
            if (e.State != InputState.Pressed) return;
            var hadAnyOpen = _manager.OpenedMenus.Count > 0;
            var screen = _manager.Coordinates.ToScreenPoints(e.Mouse.Point);
            _manager.HandleOutsideClick(screen);
            if (hadAnyOpen) e.Consume();
        }
    }
}
