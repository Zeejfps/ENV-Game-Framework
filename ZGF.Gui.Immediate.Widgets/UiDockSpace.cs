using System.Diagnostics.CodeAnalysis;
using ZGF.Desktop;
using ZGF.Geometry;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Immediate.Widgets;

/// <summary>How a panel's tab reads in a <see cref="UiWidgets.DockSpace"/>, with
/// <see cref="Tooltip"/> shown while the pointer rests on it. A panel that is not
/// <see cref="Movable"/> — an editor area the others dock around — shows no tab while alone in its
/// group, cannot be dragged, and takes other panels only beside it, not as tabs. A panel in front
/// of its group that is not <see cref="Current"/> — a document other than the one the app is
/// working in — marks its tab dimmer than one that is.</summary>
public readonly record struct DockTab(string Title, bool Closable = true, bool Movable = true, string? Tooltip = null, bool Current = true);

/// <summary>An open <see cref="UiWidgets.DockSpace"/>: call <see cref="Next"/> until it returns
/// false, recording each panel it names, then dispose.</summary>
public readonly struct DockSpace : IDisposable
{
    private readonly DockWalker? _walker;

    internal DockSpace(DockWalker walker) => _walker = walker;

    /// <summary>Records the dock space up to the next panel in view and names it: record that
    /// panel's content, then call again. Content is recorded in a <see cref="Ui.StateScope"/> named
    /// by the panel, so its widget state follows it wherever it is docked and outlives the frames
    /// its tab is hidden. False once every panel in view is recorded.</summary>
    public bool Next([NotNullWhen(true)] out string? panel)
    {
        panel = null;
        return _walker != null && _walker.Next(out panel);
    }

    public void Dispose() => _walker?.End();
}

public static partial class UiWidgets
{
    private const int DockWalkerSalt = 0x444F434B;

    /// <summary>
    /// Panels docked into splits and tab groups that fill the space given (unless
    /// <paramref name="box"/> sizes it), plus floating windows over it, as <paramref name="layout"/>
    /// arranges them. <paramref name="tab"/> names each panel's tab. The user drags a tab onto
    /// another group's tabs, into a group's middle to join it, toward a group's side or the dock
    /// space's rim to split there, or, holding Ctrl, off into a window of its own; drags a window by
    /// its title bar to dock it the same way, or to move it while Ctrl is held, and its edges to
    /// resize it; and drags dividers. Pressing a group focuses its front panel
    /// (<see cref="DockLayout.Focused"/>). Every change is written to <paramref name="layout"/> when
    /// the scope ends. Escape cancels a drag.
    /// </summary>
    public static DockSpace DockSpace(this Ui ui, string id, DockLayout layout, Func<string, DockTab> tab, Box box = default) =>
        ui.DockSpace(id, layout, tab, static (tab, panel) => tab(panel), box);

    /// <summary>A <see cref="DockSpace(Ui, string, DockLayout, Func{string, DockTab}, Box)"/> whose
    /// tabs are named by <paramref name="tab"/> from <paramref name="state"/>, so a static lambda
    /// can name them without allocating.</summary>
    public static DockSpace DockSpace<TState>(this Ui ui, string id, DockLayout layout, TState state, Func<TState, string, DockTab> tab, Box box = default)
    {
        var dockId = ui.GetId(id);
        var walker = ui.GetState<DockWalker>(HashCode.Combine(dockId, DockWalkerSalt));
        walker.Begin(ui, id, dockId, layout, walker.Tabs(state, tab), box);
        return new DockSpace(walker);
    }
}

internal interface IDockTabs
{
    DockTab Tab(string panel);
}

internal sealed class DockTabs<TState> : IDockTabs
{
    public TState State = default!;
    public Func<TState, string, DockTab> Name = null!;

    public DockTab Tab(string panel) => Name(State, panel);
}

internal sealed class DockDrag(int dock, string? panel, DockWindow? window, PointF grab, float width, float height)
{
    public int Dock { get; } = dock;
    public string? Panel { get; } = panel;
    public DockWindow? Window { get; } = window;
    public PointF Grab { get; } = grab;
    public float Width { get; } = width;
    public float Height { get; } = height;
}

internal sealed class DockWalker
{
    private const int TabsSalt = 0x54414253;
    private const int WindowZ = 10_000;
    private const int WindowZStep = 10_000;
    private const int OverlayZ = 400_000;
    private const float MinPane = 60f;
    private const float Grip = 5f;
    private const float MinWindowWidth = 160f;
    private const float MinWindowHeight = 100f;
    private const float WindowReach = 48f;
    private const float RootBand = 24f;
    private const float EdgeZone = 0.3f;
    private const float RootShare = 0.25f;
    private const float GroupShare = 0.5f;

    private enum FrameKind
    {
        Window,
        Split,
        Group,
    }

    private struct Frame
    {
        public FrameKind Kind;
        public int Stage;
        public DockNode Node;
        public DockWindow? Window;
        public SplitPane Split;
        public Ui.Scope Outer;
        public Ui.Scope Inner;
        public Ui.Scope State;
    }

    private readonly record struct GroupEntry(DockGroup Group, DockWindow? Window, int ElementId, int StripId, int FirstTab, int TabCount);

    private readonly record struct DockTarget(DockNode Node, DockEdge Edge, float Share, int Index);

    private readonly List<Frame> _frames = new();
    private readonly List<GroupEntry> _groups = new();
    private readonly List<int> _tabs = new();
    private readonly List<string> _closed = new();
    private readonly List<DockWindow> _closedWindows = new();
    private Ui _ui = null!;
    private UiTheme _theme = null!;
    private DockLayout _layout = null!;
    private IDockTabs _tab = null!;
    private IDockTabs? _source;
    private Ui.Scope _idScope;
    private Ui.Scope _root;
    private int _dockId;
    private int _nextWindow;
    private bool _open;
    private UiRect? _preview;
    private UiRect? _caret;

    public IDockTabs Tabs<TState>(TState state, Func<TState, string, DockTab> name)
    {
        if (_source is not DockTabs<TState> tabs)
            _source = tabs = new DockTabs<TState>();
        tabs.State = state;
        tabs.Name = name;
        return tabs;
    }

    public void Begin(Ui ui, string id, int dockId, DockLayout layout, IDockTabs tab, Box box)
    {
        _ui = ui;
        _theme = ui.Theme;
        _layout = layout;
        _tab = tab;
        _dockId = dockId;
        _frames.Clear();
        _groups.Clear();
        _tabs.Clear();
        _closed.Clear();
        _closedWindows.Clear();
        _preview = _caret = null;
        _nextWindow = -1;
        _open = true;

        layout.AssignIds();
        RaisePressedWindow();
        FollowDraggedWindow();

        _idScope = ui.Id(id);
        _root = ui.Box(dockId, box with
        {
            Direction = Direction.Vertical,
            Width = box.Width ?? Sizing.Grow(),
            Height = box.Height ?? Sizing.Grow(),
        });
    }

    public bool Next([NotNullWhen(true)] out string? panel)
    {
        panel = null;
        if (!_open) return false;
        while (true)
        {
            if (_frames.Count == 0)
            {
                if (_nextWindow < 0)
                {
                    _nextWindow = 0;
                    PushNode(_layout.Root, null);
                }
                else if (_nextWindow < _layout.Windows.Count)
                {
                    var window = _layout.Windows[_nextWindow++];
                    _frames.Add(new Frame { Kind = FrameKind.Window, Node = window.Root, Window = window });
                }
                else
                {
                    return false;
                }
                continue;
            }

            var top = _frames.Count - 1;
            var frame = _frames[top];
            switch (frame.Kind)
            {
                case FrameKind.Window:
                    if (frame.Stage == 0)
                    {
                        OpenWindow(ref frame, _nextWindow - 1);
                        frame.Stage = 1;
                        _frames[top] = frame;
                        PushNode(frame.Window!.Root, frame.Window);
                    }
                    else
                    {
                        frame.Inner.Dispose();
                        ResizeHandles(frame.Window!);
                        frame.Outer.Dispose();
                        _frames.RemoveAt(top);
                    }
                    break;

                case FrameKind.Split:
                    var split = (DockSplit)frame.Node;
                    switch (frame.Stage)
                    {
                        case 0:
                            var ratio = split.Ratio;
                            frame.Split = _ui.SplitPane(_ui.GetId(split.Id), split.Direction, ref ratio, MinPane, MinPane, unit: SplitUnit.Fraction);
                            _layout.Resize(split, ratio);
                            frame.Outer = frame.Split.First();
                            frame.Stage = 1;
                            _frames[top] = frame;
                            PushNode(split.First, frame.Window);
                            break;
                        case 1:
                            frame.Outer.Dispose();
                            frame.Outer = frame.Split.Second();
                            frame.Stage = 2;
                            _frames[top] = frame;
                            PushNode(split.Second, frame.Window);
                            break;
                        default:
                            frame.Outer.Dispose();
                            frame.Split.Dispose();
                            _frames.RemoveAt(top);
                            break;
                    }
                    break;

                case FrameKind.Group:
                    if (frame.Stage == 0)
                    {
                        var active = OpenGroup(ref frame);
                        if (active == null)
                        {
                            frame.Outer.Dispose();
                            _frames.RemoveAt(top);
                            break;
                        }
                        frame.Stage = 1;
                        _frames[top] = frame;
                        panel = active;
                        return true;
                    }
                    frame.State.Dispose();
                    frame.Inner.Dispose();
                    frame.Outer.Dispose();
                    _frames.RemoveAt(top);
                    break;
            }
        }
    }

    public void End()
    {
        if (!_open) return;
        while (Next(out _)) { }
        _open = false;

        var target = default(DockTarget?);
        var dragging = _ui.TryGetDrag<DockDrag>(out var drag) && drag.Dock == _dockId;
        if (dragging && _ui.Input.TakeKey(KeyboardKey.Escape))
        {
            _ui.CancelDrag();
            dragging = false;
        }
        if (dragging && _ui.TryGetRect(_dockId, out var dockRect))
        {
            var floating = (_ui.Input.Modifiers & KeyModifiers.Control) != 0;
            target = floating ? null : FindTarget(drag!, dockRect);
            if (_ui.TryDrop<DockDrag>(out _))
                Drop(drag!, target, floating, dockRect);
            else
                RecordOverlay(drag!, dockRect, target, floating);
        }

        if (_ui.Input.WasPressed(PointerButton.Left) && GroupAt(_ui.Input.Pointer, null) is { Group.Active: { } focused })
            _layout.Focus(focused);

        _root.Dispose();
        _idScope.Dispose();

        foreach (var panel in _closed)
            _layout.Close(panel);
        foreach (var window in _closedWindows)
        {
            foreach (var panel in DockLayout.PanelsIn(window.Root))
                _layout.Close(panel);
        }
        FinishMovingOnRelease();
    }

    /// <summary>Counts a splitter or window drag as one change of the layout once the button is up.</summary>
    private void FinishMovingOnRelease()
    {
        if (_ui.Input.IsDown(PointerButton.Left))
            return;
        _layout.FinishMoving();
    }

    internal static int TabId(Ui ui, string dock, string panel)
    {
        using (ui.Id(dock))
        using (ui.Id(TabsSalt))
            return ui.GetId(panel);
    }

    internal static int NodeId(Ui ui, string dock, DockNode node)
    {
        using (ui.Id(dock))
            return ui.GetId(node.Id);
    }

    internal static int WindowId(Ui ui, string dock, DockWindow window) => WindowId(ui.GetId(dock), window);

    private static int WindowId(int dockId, DockWindow window) => HashCode.Combine(dockId, "window", window.Id);

    private void PushNode(DockNode node, DockWindow? window) =>
        _frames.Add(new Frame { Kind = node is DockSplit ? FrameKind.Split : FrameKind.Group, Node = node, Window = window });

    private void RaisePressedWindow()
    {
        if (!_ui.Input.WasPressed(PointerButton.Left)) return;
        var pointer = _ui.Input.Pointer;
        for (var i = _layout.Windows.Count - 1; i >= 0; i--)
        {
            var window = _layout.Windows[i];
            if (!_ui.TryGetRect(WindowId(_dockId, window), out var rect) || !rect.Contains(pointer)) continue;
            _layout.BringToFront(window);
            return;
        }
    }

    private void FollowDraggedWindow()
    {
        if (!_ui.TryGetDrag<DockDrag>(out var drag) || drag.Dock != _dockId || drag.Window is not { } window) return;
        if (!_ui.TryGetRect(_dockId, out var dock)) return;
        var pointer = _ui.Input.Pointer;
        var moved = window.Rect with { X = pointer.X - drag.Grab.X - dock.X, Y = pointer.Y - drag.Grab.Y - dock.Y };
        _layout.Move(window, moved);
    }

    private string? OpenGroup(ref Frame frame)
    {
        var group = (DockGroup)frame.Node;
        if (group.Active == null || !group.Panels.Contains(group.Active))
            group.Active = group.Panels.Count > 0 ? group.Panels[0] : null;

        var elementId = _ui.GetId(group.Id);
        frame.Outer = _ui.Column(elementId, new Box
        {
            Width = Sizing.Grow(),
            Height = Sizing.Grow(),
            Gap = 0f,
            Clip = true,
            Background = _theme.PaneBackground,
        });

        var stripId = HashCode.Combine(elementId, "tabs");
        var firstTab = _tabs.Count;
        var fixedPanel = IsFixed(group);
        if (group.Panels.Count > 0 && !fixedPanel)
        {
            using (_ui.Id(TabsSalt))
            using (var strip = _ui.TabStrip(stripId))
            {
                for (var i = 0; i < group.Panels.Count; i++)
                {
                    var panel = group.Panels[i];
                    var info = _tab.Tab(panel);
                    var tab = strip.Tab(info.Title, panel == group.Active, info.Closable, panel, info.Tooltip, info.Movable, info.Current);
                    _tabs.Add(tab.Id);
                    if (tab.Selected)
                    {
                        _layout.SelectTab(group, panel);
                        _ui.RequestUpdate();
                    }
                    if (tab.Closed)
                        _closed.Add(panel);
                    if (tab.Dragged)
                        BeginPanelDrag(panel, elementId, tab.Id);
                }
            }
        }
        _groups.Add(new GroupEntry(group, frame.Window, elementId, fixedPanel ? 0 : stripId, firstTab, _tabs.Count - firstTab));

        foreach (var panel in group.Panels)
        {
            if (panel != group.Active)
                _ui.KeepState(panel);
        }
        if (group.Active == null) return null;

        frame.Inner = _ui.Column(new Box { Width = Sizing.Grow(), Height = Sizing.Grow(), Clip = true });
        frame.State = _ui.StateScope(group.Active);
        return group.Active;
    }

    private bool IsFixed(DockGroup group) => group.Panels.Count == 1 && !_tab.Tab(group.Panels[0]).Movable;

    private void BeginPanelDrag(string panel, int groupElementId, int tabId)
    {
        var pointer = _ui.Input.Pointer;
        var grab = _ui.TryGetRect(tabId, out var tab) ? new PointF(_ui.PressPosition.X - tab.X, _ui.PressPosition.Y - tab.Y) : new PointF(16f, 8f);
        var size = _ui.TryGetRect(groupElementId, out var group) ? group : new UiRect(pointer.X, pointer.Y, 360f, 260f);
        _ui.BeginDrag(new DockDrag(_dockId, panel, null, grab,
            Math.Clamp(size.Width, MinWindowWidth * 1.5f, 640f),
            Math.Clamp(size.Height + _theme.PaneHeaderHeight, MinWindowHeight * 1.5f, 480f)));
    }

    private void OpenWindow(ref Frame frame, int index)
    {
        var window = frame.Window!;
        if (_ui.TryGetRect(_dockId, out var dock))
        {
            window.Width = Math.Clamp(window.Width, MinWindowWidth, MathF.Max(MinWindowWidth, dock.Width));
            window.Height = Math.Clamp(window.Height, MinWindowHeight, MathF.Max(MinWindowHeight, dock.Height));
            window.X = Math.Clamp(window.X, WindowReach - window.Width, MathF.Max(0f, dock.Width - WindowReach));
            window.Y = Math.Clamp(window.Y, 0f, MathF.Max(0f, dock.Height - _theme.PaneHeaderHeight));
        }

        var windowId = WindowId(_dockId, window);
        frame.Outer = _ui.Float(0f, 0f, window.X, window.Y, _theme.PopupSurface(new Box
        {
            Direction = Direction.Vertical,
            Width = window.Width,
            Height = window.Height,
            Padding = _theme.BorderWidth,
            Background = _theme.Background,
            Radius = 0f,
        }), WindowZ + index * WindowZStep, windowId);

        TitleBar(window, windowId);
        frame.Inner = _ui.Column(new Box { Width = Sizing.Grow(), Height = Sizing.Grow(), Gap = 0f, Clip = true });
    }

    private void TitleBar(DockWindow window, int windowId)
    {
        var title = _ui.Interact(HashCode.Combine(windowId, "title"));
        if (_ui.ShouldBeginDrag(title) && _ui.TryGetRect(windowId, out var rect))
        {
            var press = _ui.PressPosition;
            _ui.BeginDrag(new DockDrag(_dockId, null, window, new PointF(press.X - rect.X, press.Y - rect.Y), rect.Width, rect.Height));
        }

        var closable = true;
        string? caption = null;
        Describe(window.Root, ref closable, ref caption);

        using (_ui.Row(title.Id, new Box
        {
            Height = _theme.PaneHeaderHeight,
            Padding = new Spacing(_theme.ShellPaddingX, 0f, _theme.GapTight, 0f),
            Gap = _theme.GapTight,
            Background = title.Held ? _theme.Pressed(_theme.SurfaceRaised) : _theme.SurfaceRaised,
        }))
        {
            _ui.Text(caption ?? "", new Box { Width = Sizing.Grow() }, color: _theme.TextMuted, bold: true);
            if (closable && _ui.CloseButton(UiWidgets.CloseButtonId(windowId), tabStop: false))
                _closedWindows.Add(window);
        }
    }

    private void Describe(DockNode node, ref bool closable, ref string? caption)
    {
        if (node is DockSplit split)
        {
            Describe(split.First, ref closable, ref caption);
            Describe(split.Second, ref closable, ref caption);
            return;
        }
        var group = (DockGroup)node;
        foreach (var panel in group.Panels)
        {
            var info = _tab.Tab(panel);
            closable &= info.Closable;
            if (panel == group.Active)
                caption ??= info.Title;
        }
    }

    private void ResizeHandles(DockWindow window)
    {
        var windowId = WindowId(_dockId, window);
        var (w, h) = (window.Width, window.Height);
        Handle(window, windowId, 1, new UiRect(0f, Grip, Grip, h - Grip * 2), MouseCursor.ResizeHorizontal);
        Handle(window, windowId, 4, new UiRect(w - Grip, Grip, Grip, h - Grip * 2), MouseCursor.ResizeHorizontal);
        Handle(window, windowId, 2, new UiRect(Grip, 0f, w - Grip * 2, Grip), MouseCursor.ResizeVertical);
        Handle(window, windowId, 8, new UiRect(Grip, h - Grip, w - Grip * 2, Grip), MouseCursor.ResizeVertical);
        Handle(window, windowId, 1 | 2, new UiRect(0f, 0f, Grip * 2, Grip * 2), MouseCursor.ResizeNwse);
        Handle(window, windowId, 4 | 8, new UiRect(w - Grip * 2, h - Grip * 2, Grip * 2, Grip * 2), MouseCursor.ResizeNwse);
        Handle(window, windowId, 4 | 2, new UiRect(w - Grip * 2, 0f, Grip * 2, Grip * 2), MouseCursor.ResizeNesw);
        Handle(window, windowId, 1 | 8, new UiRect(0f, h - Grip * 2, Grip * 2, Grip * 2), MouseCursor.ResizeNesw);
    }

    private void Handle(DockWindow window, int windowId, int edges, UiRect at, MouseCursor cursor)
    {
        var handle = _ui.Interact(HashCode.Combine(windowId, "resize", edges));
        if (handle.Hovered || handle.Held)
            _ui.SetCursor(cursor);
        if (_ui.TryGetDragStart(handle, window.Rect, out var start))
            Resize(window, start, edges);

        using (_ui.Float(0f, 0f, at.X, at.Y, new Box { Width = at.Width, Height = at.Height }, 2, handle.Id)) { }
    }

    private void Resize(DockWindow window, UiRect start, int edges)
    {
        var pointer = _ui.Input.Pointer;
        var press = _ui.PressPosition;
        var dx = pointer.X - press.X;
        var dy = pointer.Y - press.Y;
        var (x, y, w, h) = (start.X, start.Y, start.Width, start.Height);
        if ((edges & 1) != 0)
        {
            var width = MathF.Max(MinWindowWidth, w - dx);
            x += w - width;
            w = width;
        }
        if ((edges & 4) != 0)
            w = MathF.Max(MinWindowWidth, w + dx);
        if ((edges & 2) != 0)
        {
            var height = MathF.Max(MinWindowHeight, h - dy);
            y += h - height;
            h = height;
        }
        if ((edges & 8) != 0)
            h = MathF.Max(MinWindowHeight, h + dy);
        _layout.Move(window, new UiRect(x, y, w, h));
    }

    private DockTarget? FindTarget(DockDrag drag, UiRect dock)
    {
        var pointer = _ui.Input.Pointer;
        var source = drag.Panel != null ? _layout.GroupOf(drag.Panel) : null;
        var over = GroupAt(pointer, drag.Window);
        var rect = default(UiRect);
        if (over is { } hit && !_ui.TryGetRect(hit.ElementId, out rect))
            over = null;

        if (over is { StripId: not 0 } onStrip && _ui.TryGetRect(onStrip.StripId, out var strip) && strip.Contains(pointer))
        {
            var index = onStrip.Group.Panels.Count;
            var caretX = strip.X;
            for (var i = 0; i < onStrip.TabCount; i++)
            {
                if (!_ui.TryGetRect(_tabs[onStrip.FirstTab + i], out var tab)) continue;
                caretX = tab.Right;
                if (pointer.X >= tab.X + tab.Width / 2) continue;
                index = i;
                caretX = tab.X;
                break;
            }
            _caret = new UiRect(caretX - 1f, strip.Y + 3f, 2f, strip.Height - 6f);
            _preview = rect;
            return new DockTarget(onStrip.Group, DockEdge.Center, GroupShare, index);
        }

        if (over is not { Window: not null } && _layout.Root is not DockGroup { Panels.Count: 0 })
        {
            var inset = new UiRect(dock.X + RootBand, dock.Y + RootBand, dock.Width - RootBand * 2, dock.Height - RootBand * 2);
            if (dock.Contains(pointer) && !inset.Contains(pointer))
            {
                var rootEdge = NearestEdge(dock, pointer, out _);
                _preview = Part(dock, rootEdge, RootShare);
                return new DockTarget(_layout.Root, rootEdge, RootShare, -1);
            }
        }

        if (over is not { } entry) return null;
        var group = entry.Group;
        var alone = source == group && group.Panels.Count == 1;
        if (alone && _layout.NewPanel == null) return null;
        var edge = NearestEdge(rect, pointer, out var depth);
        if (group.Panels.Count == 0 || (entry.StripId != 0 && !alone && depth > EdgeZone))
            edge = DockEdge.Center;
        _preview = Part(rect, edge, GroupShare);
        return new DockTarget(group, edge, GroupShare, -1);
    }

    private static DockEdge NearestEdge(UiRect rect, PointF pointer, out float depth)
    {
        var u = Math.Clamp((pointer.X - rect.X) / MathF.Max(1f, rect.Width), 0f, 1f);
        var v = Math.Clamp((pointer.Y - rect.Y) / MathF.Max(1f, rect.Height), 0f, 1f);
        var (edge, x) = u < 0.5f ? (DockEdge.Left, u) : (DockEdge.Right, 1f - u);
        var (vertical, y) = v < 0.5f ? (DockEdge.Top, v) : (DockEdge.Bottom, 1f - v);
        if (y < x)
            (edge, x) = (vertical, y);
        depth = x;
        return edge;
    }

    private GroupEntry? GroupAt(PointF pointer, DockWindow? skip)
    {
        DockWindow? over = null;
        for (var i = _layout.Windows.Count - 1; i >= 0; i--)
        {
            var window = _layout.Windows[i];
            if (window == skip) continue;
            if (!_ui.TryGetRect(WindowId(_dockId, window), out var rect) || !rect.Contains(pointer)) continue;
            over = window;
            break;
        }
        foreach (var entry in _groups)
        {
            if (entry.Window != over) continue;
            if (_ui.TryGetRect(entry.ElementId, out var rect) && rect.Contains(pointer))
                return entry;
        }
        return null;
    }

    private static UiRect Part(UiRect rect, DockEdge edge, float share) => edge switch
    {
        DockEdge.Left => rect with { Width = rect.Width * share },
        DockEdge.Right => rect with { X = rect.Right - rect.Width * share, Width = rect.Width * share },
        DockEdge.Top => rect with { Height = rect.Height * share },
        DockEdge.Bottom => rect with { Y = rect.Bottom - rect.Height * share, Height = rect.Height * share },
        _ => rect,
    };

    private UiRect FloatingRect(DockDrag drag, UiRect dock)
    {
        var pointer = _ui.Input.Pointer;
        return new UiRect(pointer.X - drag.Grab.X - dock.X, pointer.Y - drag.Grab.Y - dock.Y, drag.Width, drag.Height);
    }

    private void Drop(DockDrag drag, DockTarget? target, bool floating, UiRect dock)
    {
        if (target is { } t)
        {
            if (drag.Window != null)
                _layout.Dock(drag.Window, t.Node, t.Edge, t.Share, t.Index);
            else
                _layout.Dock(drag.Panel!, t.Node, t.Edge, t.Share, t.Index);
        }
        else if (floating && drag.Panel != null)
        {
            _layout.Float(drag.Panel, FloatingRect(drag, dock));
        }
        _ui.RequestUpdate();
    }

    private void RecordOverlay(DockDrag drag, UiRect dock, DockTarget? target, bool floating)
    {
        if (floating && drag.Panel != null)
        {
            var ghost = FloatingRect(drag, dock);
            _preview = ghost with { X = ghost.X + dock.X, Y = ghost.Y + dock.Y };
        }
        using (_ui.Float(0f, 0f, 0f, 0f, new Box { Width = Sizing.Grow(), Height = Sizing.Grow() }, OverlayZ))
            _ui.Paint(this, static (p, walker) => walker.PaintOverlay(p));
        _ui.RequestUpdate();
    }

    private void PaintOverlay(UiPainter p)
    {
        var accent = _theme.Accent;
        if (_preview is { } preview)
            p.FillRect(preview, (accent & 0x00FFFFFFu) | 0x40000000u, 0f, accent, _theme.BorderWidth);
        if (_caret is { } caret)
            p.FillRect(caret, accent);
    }
}
