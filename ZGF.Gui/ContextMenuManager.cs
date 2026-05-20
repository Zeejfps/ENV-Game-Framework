using ZGF.Geometry;

namespace ZGF.Gui.Tests;

public interface IOpenedContextMenu
{
    event Action Closed;
    MultiChildView View { get; }
    bool IsOpened { get; }

    void CancelCloseRequest();
    void CloseRequest();
}

sealed class OpenedContextMenu : IOpenedContextMenu
{
    public event Action? Closed;

    public bool IsOpened { get; private set; } = true;
    public required ContextMenu ContextMenu { get; init; }
    public OpenedContextMenu? Parent { get; set; }
    public OpenedContextMenu? Child { get; set; }
    public long CloseTimestamp { get; set; }
    public MultiChildView View => ContextMenu;

    public bool IsCloseRequested { get; private set; }

    public void CancelCloseRequest()
    {
        if (!IsOpened)
            return;

        IsCloseRequested = false;
    }

    public void CloseRequest()
    {
        IsCloseRequested = true;
    }

    public void Close()
    {
        IsOpened = false;
        Closed?.Invoke();
    }
}

public sealed class ContextMenuManager
{
    private readonly MultiChildView _contextMenuPane;
    private readonly InputSystem? _inputSystem;
    private readonly OutsideClickController _outsideClickController;
    private bool _outsideClickActive;

    private readonly HashSet<OpenedContextMenu> _closingMenus = new();
    private readonly Dictionary<ContextMenu, OpenedContextMenu> _openedMenus = new();

    public ContextMenuManager(MultiChildView contextMenuPane, InputSystem? inputSystem = null)
    {
        _contextMenuPane = contextMenuPane;
        _inputSystem = inputSystem;
        _outsideClickController = new OutsideClickController(this);
    }

    public IOpenedContextMenu? ShowContextMenu(ContextMenu contextMenu, ContextMenu? parentMenu = null)
    {
        if (_openedMenus.TryGetValue(contextMenu, out var openedMenu))
        {
            throw new Exception("Menu already opened");
        }

        openedMenu = new OpenedContextMenu
        {
            ContextMenu = contextMenu,
        };

        if (parentMenu != null)
        {
            if (!_openedMenus.TryGetValue(parentMenu, out var openedParentMenu))
            {
                return null;
            }
            openedParentMenu.Child = openedMenu;
            openedMenu.Parent = openedParentMenu;
        }

        _openedMenus[contextMenu] = openedMenu;
        _contextMenuPane.Children.Add(contextMenu);
        EnsureOutsideClickHandler();
        return openedMenu;
    }

    public void Update()
    {
        var now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        foreach (var menu in _closingMenus.ToList())
        {
            var component = menu.ContextMenu;
            var timestamp = menu.CloseTimestamp;
            if (now - timestamp > 100)
            {
                //Console.WriteLine($"Closing: {menu.GetHashCode()}");
                _contextMenuPane.Children.Remove(component);
                _closingMenus.Remove(menu);
                _openedMenus.Remove(component);
                menu.Close();
            }
        }

        foreach (var contextMenu in _openedMenus.Values.ToList())
        {
            if (contextMenu.IsCloseRequested && contextMenu.Child == null)
            {
                contextMenu.CloseTimestamp = now;
                _closingMenus.Add(contextMenu);
                _openedMenus.Remove(contextMenu.ContextMenu);

                var parent = contextMenu.Parent;
                if (parent != null)
                {
                    parent.Child = null;
                }
            }
        }

        ReleaseOutsideClickHandlerIfEmpty();
    }

    public void RequestCloseMenu(ContextMenu contextMenu)
    {
        if (_openedMenus.TryGetValue(contextMenu, out var openedMenu))
        {
            openedMenu.CloseRequest();
        }
    }

    /// <summary>
    /// Closes every open or closing menu right now, skipping the standard 100 ms
    /// linger window. Use when a fresh menu interaction needs to wipe whatever was
    /// previously on screen (e.g. right-clicking again before the old menu fades).
    /// </summary>
    public void CloseAllImmediately()
    {
        foreach (var menu in _closingMenus.ToList())
        {
            _contextMenuPane.Children.Remove(menu.ContextMenu);
            menu.Close();
        }
        _closingMenus.Clear();

        foreach (var opened in _openedMenus.Values.ToList())
        {
            _contextMenuPane.Children.Remove(opened.ContextMenu);
            var parent = opened.Parent;
            if (parent != null) parent.Child = null;
            opened.Close();
        }
        _openedMenus.Clear();
        ReleaseOutsideClickHandlerIfEmpty();
    }

    private void EnsureOutsideClickHandler()
    {
        if (_outsideClickActive) return;
        if (_inputSystem == null) return;
        _inputSystem.StealFocus(_outsideClickController);
        _outsideClickActive = true;
    }

    private void ReleaseOutsideClickHandlerIfEmpty()
    {
        if (!_outsideClickActive) return;
        if (_openedMenus.Count > 0) return;
        if (_closingMenus.Count > 0) return;
        _inputSystem?.Blur(_outsideClickController);
        _outsideClickActive = false;
    }

    // Called by OutsideClickController when a mouse-press lands while at least one
    // menu is open. Returns true if the click hit the empty space outside every
    // open menu and we dismissed them — caller consumes the event in that case so
    // the dismissing click doesn't also activate the UI underneath. Clicks inside
    // a menu return false so the per-item controllers handle the selection.
    internal bool HandleOutsideClick(in PointF point)
    {
        foreach (var opened in _openedMenus.Values)
        {
            if (opened.ContextMenu.Position.ContainsPoint(point)) return false;
        }
        CloseAllImmediately();
        return true;
    }

    private sealed class OutsideClickController : KeyboardMouseController
    {
        private readonly ContextMenuManager _manager;
        public OutsideClickController(ContextMenuManager manager) { _manager = manager; }

        public override void OnMouseButtonStateChanged(ref MouseButtonEvent e)
        {
            if (e.State != InputState.Pressed) return;
            if (_manager.HandleOutsideClick(e.Mouse.Point))
                e.Consume();
        }
    }
}