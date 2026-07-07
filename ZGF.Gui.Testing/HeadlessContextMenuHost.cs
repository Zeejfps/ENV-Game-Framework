using ZGF.Geometry;
using ZGF.Gui.Desktop;
using ZGF.Gui.Desktop.Components.ContextMenu;
using ZGF.Gui.Desktop.Input;

namespace ZGF.Gui.Testing;

/// <summary>One open menu the harness can inspect and drive: its root <see cref="ContextMenu"/>
/// view and the <see cref="InputSystem"/> that owns its controllers.</summary>
public readonly record struct OpenMenu(ContextMenu Menu, InputSystem Input);

/// <summary>An <see cref="IContextMenuHost"/> that opens context menus as in-memory mounted roots
/// instead of native popup windows, so <see cref="GuiTestHarness"/> can snapshot and click them
/// headlessly. Each menu gets its own <see cref="InputSystem"/> and a child <see cref="Context"/>
/// (canvas inherited from the harness, plus this host and an identity <see cref="IWindowCoordinates"/>),
/// mirroring the per-window context a real popup builds against — so the same <c>ContextMenu</c>
/// views and controllers run unchanged. Closes are deferred: a leaf item click calls
/// <see cref="RequestCloseAll"/> mid-dispatch, so teardown is applied lazily on the next read rather
/// than while a menu's own input event is dispatching.</summary>
public sealed class HeadlessContextMenuHost : IContextMenuHost
{
    private sealed class Entry
    {
        public required ContextMenu Menu { get; init; }
        public required Context Context { get; init; }
        public required InputSystem Input { get; init; }
        public required HeadlessOpenedMenu Opened { get; init; }
        public bool ClosePending { get; set; }
    }

    private readonly Context _parent;
    private readonly List<Entry> _open = new();
    private readonly List<Action> _afterCloseActions = new();
    private bool _applying;

    public HeadlessContextMenuHost(Context parent)
    {
        _parent = parent;
    }

    /// <summary>The currently open menus, oldest first (last = topmost submenu). Pending closes are
    /// applied first, so a menu dismissed by a click is already gone here.</summary>
    public IReadOnlyList<OpenMenu> OpenMenus
    {
        get
        {
            ApplyPendingCloses();
            var list = new List<OpenMenu>(_open.Count);
            foreach (var e in _open) list.Add(new OpenMenu(e.Menu, e.Input));
            return list;
        }
    }

    public IOpenedContextMenu? ShowContextMenu(
        Func<Context, ContextMenu> buildMenu, PointI screenAnchor,
        ContextMenu? parentMenu = null, MenuPlacement placement = MenuPlacement.Below)
    {
        ApplyPendingCloses();

        // The menu's own per-window context: its input system (controllers register here), this host
        // (so item controllers can RequestCloseAll and open submenus), and identity coordinates for
        // submenu anchoring. Canvas is inherited from the harness context by Context(parent).
        var input = new InputSystem();
        var ctx = new Context(_parent);
        ctx.AddService(input);
        ctx.AddService<IContextMenuHost>(this);
        ctx.AddService<IWindowCoordinates>(IdentityWindowCoordinates.Instance);

        var menu = buildMenu(ctx);
        var opened = new HeadlessOpenedMenu(menu, ctx, this);
        menu.OnRedrawNeeded = static () => { };
        menu.Mount();
        menu.LayoutSelf();

        _open.Add(new Entry { Menu = menu, Context = ctx, Input = input, Opened = opened });
        return opened;
    }

    public void RequestCloseMenu(ContextMenu menu)
    {
        foreach (var e in _open)
            if (ReferenceEquals(e.Menu, menu)) e.ClosePending = true;
    }

    public void RequestCloseAll()
    {
        foreach (var e in _open) e.ClosePending = true;
    }

    public void CloseAllAndThen(Action afterClosed)
    {
        // Flag every menu for close and defer the action, mirroring the desktop host: the
        // teardown and the action both run on the next ApplyPendingCloses (a read), never
        // mid-dispatch, and the action runs only after the menus are gone.
        foreach (var e in _open) e.ClosePending = true;
        _afterCloseActions.Add(afterClosed);
    }

    public void CloseAllImmediately()
    {
        // Synchronous teardown — only called outside input dispatch (e.g. before opening a fresh
        // menu). Topmost (child) first so a submenu tears down before its parent.
        for (var i = _open.Count - 1; i >= 0; i--)
        {
            var e = _open[i];
            _open.RemoveAt(i);
            Teardown(e);
        }
    }

    internal void CancelClose(ContextMenu menu)
    {
        foreach (var e in _open)
            if (ReferenceEquals(e.Menu, menu)) e.ClosePending = false;
    }

    // Tear down every menu flagged for close, topmost (child) first, firing its Closed event.
    // Reentrancy-guarded: a Closed handler may read OpenMenus, which re-enters here.
    internal void ApplyPendingCloses()
    {
        if (_applying) return;
        _applying = true;
        try
        {
            for (var i = _open.Count - 1; i >= 0; i--)
            {
                if (!_open[i].ClosePending) continue;
                var e = _open[i];
                _open.RemoveAt(i);
                Teardown(e);
            }

            // Menus are torn down — run any actions that were waiting for the close
            // (CloseAllAndThen). Re-entrant reads are guarded above, so this runs after the
            // menu is gone from every observer's point of view.
            if (_afterCloseActions.Count > 0)
            {
                var actions = _afterCloseActions.ToArray();
                _afterCloseActions.Clear();
                foreach (var action in actions) action();
            }
        }
        finally
        {
            _applying = false;
        }
    }

    private static void Teardown(Entry e)
    {
        e.Menu.Unmount();
        e.Menu.OnRedrawNeeded = null;
        e.Opened.RaiseClosed();
        e.Context.Dispose();
    }
}

internal sealed class HeadlessOpenedMenu : IOpenedContextMenu
{
    private readonly HeadlessContextMenuHost _host;
    private bool _opened = true;

    public HeadlessOpenedMenu(ContextMenu menu, Context context, HeadlessContextMenuHost host)
    {
        Menu = menu;
        Context = context;
        _host = host;
    }

    public event Action? Closed;
    public bool IsOpened => _opened;
    public ContextMenu Menu { get; }
    public Context Context { get; }

    public void CancelCloseRequest() => _host.CancelClose(Menu);
    public void CloseRequest() => _host.RequestCloseMenu(Menu);

    internal void RaiseClosed()
    {
        if (!_opened) return;
        _opened = false;
        Closed?.Invoke();
    }
}

/// <summary>A no-op <see cref="IWindowCoordinates"/> for headless menus: canvas points are screen
/// points (rounded). There's no real window offset or DPI to apply, and placement is irrelevant when
/// each menu is its own in-memory coordinate space.</summary>
internal sealed class IdentityWindowCoordinates : IWindowCoordinates
{
    public static readonly IdentityWindowCoordinates Instance = new();

    public PointI ToScreenPoints(PointF canvasPoint) =>
        new((int)MathF.Round(canvasPoint.X), (int)MathF.Round(canvasPoint.Y));

    public RectI ToScreenPoints(RectF canvasRect) =>
        new((int)MathF.Round(canvasRect.Left), (int)MathF.Round(canvasRect.Bottom),
            (int)MathF.Round(canvasRect.Width), (int)MathF.Round(canvasRect.Height));
}
