using ZGF.Geometry;
using ZGF.Gui.Desktop.Components.ContextMenu;
using ZGF.Gui.Desktop.Input;
using InputState = ZGF.Gui.Desktop.Input.InputState;

namespace ZGF.Gui.Desktop;

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

public sealed class ContextMenuManager : IContextMenuHost
{
    private readonly IPopupWindowFactory _popupFactory;
    private readonly IWindowCoordinates _coordinates;
    private readonly Context? _measureContext;

    private readonly HashSet<OpenedContextMenu> _closingMenus = new();
    private readonly Dictionary<ContextMenu, OpenedContextMenu> _openedMenus = new();
    // Logical close (dict removal, Closed event) happens synchronously so callers
    // that immediately open a new menu see an empty state. The actual OS-level
    // Window.Hide + decorator.EndCapture is deferred to Update() so we don't
    // hide a popup window from inside its own WND_PROC subclass — which on
    // Windows leaves activation/capture state half-broken.
    private readonly List<OpenedContextMenu> _pendingHide = new();

    public ContextMenuManager(
        IPopupWindowFactory popupFactory,
        IWindowCoordinates coordinates,
        DesktopInputSystem? mainInput = null,
        Context? measureContext = null)
    {
        _popupFactory = popupFactory;
        _coordinates = coordinates;
        _measureContext = measureContext;
        // Outside-click fallback for platforms whose decorator provides no OS-level
        // capture (e.g. NoopPopupDecorator): a press on the main window while a menu is
        // open is, by definition, outside the menu. Hover modality is handled entirely
        // by the PointerOwnershipArbiter, so this controller no longer touches focus.
        if (mainInput != null)
            mainInput.OnMouseButtonPreview += OnMainWindowPress;
    }

    private void OnMainWindowPress(MouseButtonEvent e)
    {
        if (e.State != InputState.Pressed) return;
        if (_openedMenus.Count == 0 && _closingMenus.Count == 0) return;
        var screen = _coordinates.ToScreenPoints(e.Mouse.Point);
        HandleOutsideClick(screen);
    }

    public IOpenedContextMenu? ShowContextMenu(ContextMenu menu, PointI screenAnchor, ContextMenu? parentMenu = null, MenuPlacement placement = MenuPlacement.Below)
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

        // Below: the menu's top sits at the anchor and it grows down (the default — anchor is
        // the trigger's bottom edge). Above: the menu's bottom sits at the anchor and it grows
        // up (anchor is the trigger's top edge), for triggers near the bottom of the screen.
        // The other direction becomes the flip fallback the popup factory uses when the
        // preferred placement won't fit on the monitor.
        var belowRect = new RectI(X: screenAnchor.X, Y: screenAnchor.Y, Width: width, Height: height);
        var aboveRect = belowRect with { Y = screenAnchor.Y - height };
        var (preferred, flipped) = placement == MenuPlacement.Above
            ? (aboveRect, belowRect)
            : (belowRect, aboveRect);

        var popup = _popupFactory.Acquire(new PopupRequest
        {
            Root = menu,
            PreferredScreenRect = preferred,
            FlippedScreenRect = flipped,
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
        return opened;
    }

    public void Update()
    {
        if (_pendingHide.Count > 0)
        {
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
    }

    public void RequestCloseMenu(ContextMenu menu)
    {
        if (_openedMenus.TryGetValue(menu, out var opened))
            opened.CloseRequest();
    }

    // Deferred close of every open menu (the whole parent→submenu chain). Only flags
    // each menu; the actual teardown happens in Update(). Unlike CloseAllImmediately
    // this performs no structural mutation, so it is safe to call from inside an input
    // event dispatch — e.g. clicking a submenu item, where synchronously unregistering
    // controllers would mutate the input system's focus queue mid-iteration.
    public void RequestCloseAll()
    {
        foreach (var opened in _openedMenus.Values)
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
    }

    internal void HandleOutsideClick(PointI screenPoint)
    {
        foreach (var opened in _openedMenus.Values)
        {
            var window = opened.Popup.Window;
            window.GetPosition(out var x, out var y);
            var w = window.Width;
            var h = window.Height;
            if (screenPoint.X >= x && screenPoint.X < x + w &&
                screenPoint.Y >= y && screenPoint.Y < y + h)
            {
                return;
            }
        }
        CloseAllImmediately();
    }

    internal IWindowCoordinates Coordinates => _coordinates;
    internal IReadOnlyCollection<OpenedContextMenu> OpenedMenus => _openedMenus.Values;
}
