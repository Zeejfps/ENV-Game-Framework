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
    private readonly Context? _measureContext;
    private readonly MainWindowOutsideClickController _outsideClickController;
    private bool _outsideClickActive;

    private readonly HashSet<OpenedContextMenu> _closingMenus = new();
    private readonly Dictionary<ContextMenu, OpenedContextMenu> _openedMenus = new();
    // Logical close (dict removal, Closed event) happens synchronously so callers
    // that immediately open a new menu see an empty state. The actual OS-level
    // Glfw.HideWindow + decorator.EndCapture is deferred to Update() so we don't
    // hide a popup window from inside its own WND_PROC subclass — which on
    // Windows leaves activation/capture state half-broken.
    private readonly List<OpenedContextMenu> _pendingHide = new();

    public ContextMenuManager(
        IPopupWindowFactory popupFactory,
        IWindowCoordinates coordinates,
        InputSystem? mainInputSystem = null,
        Context? measureContext = null)
    {
        _popupFactory = popupFactory;
        _coordinates = coordinates;
        _mainInputSystem = mainInputSystem;
        _measureContext = measureContext;
        _outsideClickController = new MainWindowOutsideClickController(this);
    }

    public IOpenedContextMenu? ShowContextMenu(ContextMenu menu, PointF canvasAnchor, ContextMenu? parentMenu = null)
    {
        if (_openedMenus.ContainsKey(menu))
            throw new System.Exception("Menu already opened");

        // MeasureWidth/Height walks the view tree which needs a context (for canvas
        // + fonts) to compute text metrics. Without this, the popup is sized at
        // whatever default the views fall back to (often zero or tiny) and the
        // resulting popup truncates its own content. Attach to the main context
        // for measurement; PopupWindowImpl.SetRoot reassigns to the popup context.
        if (_measureContext != null && menu.Context == null)
        {
            menu.Context = _measureContext;
        }
        var width = (int)MathF.Ceiling(menu.MeasureWidth());
        var height = (int)MathF.Ceiling(menu.MeasureHeight(width));
        PopupDebugLog.Log(PopupDebugLog.Channel.Layout,
            $"ShowContextMenu: measured {width}x{height} (measureContext={(_measureContext != null ? "yes" : "no")})");
        var screenAnchor = _coordinates.ToScreenPoints(canvasAnchor);
        PopupDebugLog.Log(PopupDebugLog.Channel.Layout,
            $"ShowContextMenu: anchor canvas={canvasAnchor} screen=({screenAnchor.X},{screenAnchor.Y})");

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
        if (_pendingHide.Count > 0)
        {
            PopupDebugLog.Log(PopupDebugLog.Channel.Outside,
                $"Update: draining {_pendingHide.Count} pending hide(s)");
            foreach (var menu in _pendingHide)
            {
                _popupFactory.Release(menu.Popup);
            }
            _pendingHide.Clear();
        }

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
        // Logical close is synchronous (so a caller that immediately opens a new
        // menu sees an empty state), but OS-level Hide+EndCapture is deferred to
        // Update() via _pendingHide. See _pendingHide field comment.
        foreach (var menu in _closingMenus)
        {
            _pendingHide.Add(menu);
            menu.Close();
        }
        _closingMenus.Clear();

        foreach (var opened in _openedMenus.Values.ToList())
        {
            var parent = opened.Parent;
            if (parent != null) parent.Child = null;
            _pendingHide.Add(opened);
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
        PopupDebugLog.Log(PopupDebugLog.Channel.Outside,
            $"HandleOutsideClick: screen=({screenPoint.X},{screenPoint.Y}) opened={_openedMenus.Count}");
        foreach (var opened in _openedMenus.Values)
        {
            var window = (Window)opened.Popup.Window.WindowHandle;
            Glfw.GetWindowPosition(window, out var x, out var y);
            Glfw.GetWindowSize(window, out var w, out var h);
            PopupDebugLog.Log(PopupDebugLog.Channel.Outside,
                $"HandleOutsideClick: check popup rect=({x},{y} {w}x{h})");
            if (screenPoint.X >= x && screenPoint.X < x + w &&
                screenPoint.Y >= y && screenPoint.Y < y + h)
            {
                PopupDebugLog.Log(PopupDebugLog.Channel.Outside, "HandleOutsideClick: inside an open popup — no dismiss");
                return;
            }
        }
        PopupDebugLog.Log(PopupDebugLog.Channel.Outside, "HandleOutsideClick: outside all popups — CloseAllImmediately (deferred Hide)");
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
            // Do NOT consume — let the press propagate so a right-click on a
            // different repo row (while a menu is already open) can both close
            // the old menu and open a new one in a single click. The logical
            // close is synchronous (dict cleared via CloseAllImmediately), so
            // RepoBarContextMenu.Show downstream sees no opened menus.
            var screen = _manager.Coordinates.ToScreenPoints(e.Mouse.Point);
            _manager.HandleOutsideClick(screen);
        }
    }
}
