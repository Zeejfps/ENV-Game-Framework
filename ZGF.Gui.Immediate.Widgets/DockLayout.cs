using System.Text.Json.Serialization;

namespace ZGF.Gui.Immediate.Widgets;

/// <summary>Where a panel or window goes when docked against a node: into it as a tab
/// (<see cref="Center"/>, groups only), or beside it, splitting its space.</summary>
public enum DockEdge
{
    Center,
    Left,
    Top,
    Right,
    Bottom,
}

/// <summary>A node of a <see cref="DockLayout"/>: a <see cref="DockSplit"/> or a
/// <see cref="DockGroup"/>. <see cref="Id"/> is given by the layout and kept across saves, so
/// splitters and tab strips keep their state while the layout changes around them.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(DockSplit), "split")]
[JsonDerivedType(typeof(DockGroup), "group")]
public abstract class DockNode
{
    public int Id { get; set; }
}

/// <summary>Two nodes side by side (<see cref="Direction.Horizontal"/>) or stacked;
/// <see cref="Ratio"/> is the first one's share of the space.</summary>
public sealed class DockSplit : DockNode
{
    public Direction Direction { get; set; }
    public float Ratio { get; set; } = 0.5f;
    public DockNode First { get; set; } = new DockGroup();
    public DockNode Second { get; set; } = new DockGroup();
}

/// <summary>Panels shown as tabs, <see cref="Active"/> in front.</summary>
public sealed class DockGroup : DockNode
{
    public List<string> Panels { get; set; } = new();
    public string? Active { get; set; }
}

/// <summary>A floating window over the dock space, in its coordinates, holding a node tree of
/// its own.</summary>
public sealed class DockWindow
{
    public int Id { get; set; }
    public DockNode Root { get; set; } = new DockGroup();
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }

    [JsonIgnore]
    public UiRect Rect
    {
        get => new(X, Y, Width, Height);
        set => (X, Y, Width, Height) = (value.X, value.Y, value.Width, value.Height);
    }
}

/// <summary>
/// How the panels of a <see cref="UiWidgets.DockSpace"/> are arranged: a tree of splits and tab
/// groups filling the dock space, plus floating windows, back to front. Panels are named by the
/// app's ids. Owned by the app like any other state, and serializable, so the arrangement can be
/// saved; the dock space rearranges it as the user drags tabs and windows around.
/// </summary>
public sealed class DockLayout
{
    public DockNode Root { get; set; } = new DockGroup();
    public List<DockWindow> Windows { get; set; } = new();
    public int NextId { get; set; } = 1;

    /// <summary>The panel the user last worked in: the front panel of the group last pressed, or the
    /// panel last activated, docked or floated. Moves to the panel in front of it when it closes.</summary>
    [JsonIgnore]
    public string? Focused { get; private set; }

    /// <summary>Makes a new panel to stay behind when a group's only panel moves out of a group that
    /// must not go away: the group it is docked beside, which is then split, with the new panel
    /// focused; or the root group. Without it, docking a group's only panel beside the group does
    /// nothing and moving the root's only panel away leaves the root empty.</summary>
    [JsonIgnore]
    public Func<string>? NewPanel { get; set; }

    /// <summary>Raised when the user closes a panel, or <see cref="Close"/> is called.</summary>
    public event Action<string>? PanelClosed;

    public static DockGroup Group(params string[] panels) => new() { Panels = panels.ToList(), Active = panels.FirstOrDefault() };

    public static DockSplit Split(Direction direction, float ratio, DockNode first, DockNode second) =>
        new() { Direction = direction, Ratio = ratio, First = first, Second = second };

    public bool Contains(string panel) => GroupOf(panel) != null;

    /// <summary>The group showing <paramref name="panel"/>, docked or floating.</summary>
    public DockGroup? GroupOf(string panel)
    {
        var group = GroupOf(Root, panel);
        foreach (var window in Windows)
            group ??= GroupOf(window.Root, panel);
        return group;
    }

    /// <summary>The window holding <paramref name="node"/>, or null when it is docked.</summary>
    public DockWindow? WindowOf(DockNode node)
    {
        foreach (var window in Windows)
        {
            if (Holds(window.Root, node))
                return window;
        }
        return null;
    }

    /// <summary>Brings <paramref name="panel"/> to the front of its group, and its window to the
    /// front of the others, and focuses it.</summary>
    public void Activate(string panel)
    {
        if (GroupOf(panel) is not { } group) return;
        Focused = panel;
        var changed = Select(group, panel);
        if (WindowOf(group) is { } window && Raise(window))
            changed = true;
        if (changed)
            Changes++;
    }

    public void BringToFront(DockWindow window)
    {
        if (Raise(window))
            Changes++;
    }

    /// <summary>Puts <paramref name="window"/> in front of the others; false when it was.</summary>
    private bool Raise(DockWindow window)
    {
        if (Windows.Count == 0 || Windows[^1] == window || !Windows.Remove(window)) return false;
        Windows.Add(window);
        return true;
    }

    /// <summary>Brings the tab the user picked in <paramref name="group"/>'s strip to the front, counting
    /// it as a change when another was in front.</summary>
    internal void SelectTab(DockGroup group, string panel)
    {
        if (Select(group, panel))
            Changes++;
    }

    private static bool Select(DockGroup group, string panel)
    {
        if (group.Active == panel)
            return false;
        group.Active = panel;
        return true;
    }

    /// <summary>Counts the changes made to the arrangement: by the methods that rearrange it, and once
    /// for each splitter or window drag when it ends, never on the frames in between. Watch it to
    /// tell when the arrangement should be saved.</summary>
    [JsonIgnore]
    public int Changes { get; private set; }

    private bool _isMoving;

    /// <summary>Gives <paramref name="split"/> the ratio a dragged splitter sets, noting the drag when it moved.</summary>
    internal void Resize(DockSplit split, float ratio)
    {
        if (ratio == split.Ratio)
            return;
        split.Ratio = ratio;
        _isMoving = true;
    }

    /// <summary>Gives <paramref name="window"/> the place a drag sets, noting the drag when it moved.</summary>
    internal void Move(DockWindow window, UiRect rect)
    {
        if (window.Rect == rect)
            return;
        window.Rect = rect;
        _isMoving = true;
    }

    /// <summary>Counts the drag that was moving a splitter or a window as one change, once it ended.</summary>
    internal void FinishMoving()
    {
        if (!_isMoving)
            return;
        _isMoving = false;
        Changes++;
    }

    /// <summary>Removes <paramref name="panel"/>, collapsing what it leaves empty, and raises
    /// <see cref="PanelClosed"/>.</summary>
    public void Close(string panel)
    {
        if (!Detach(panel, out _, out _)) return;
        Changes++;
        PanelClosed?.Invoke(panel);
    }

    /// <summary>Moves <paramref name="panel"/>, or adds it when not shown, into or beside
    /// <paramref name="target"/>. A new split gives the panel <paramref name="share"/> of the space;
    /// a tab goes at <paramref name="index"/>, or last.</summary>
    public void Dock(string panel, DockNode target, DockEdge edge, float share = 0.5f, int index = -1)
    {
        if (DockPanel(panel, target, edge, share, index))
            Changes++;
    }

    /// <summary>Docks <paramref name="panel"/> as <see cref="Dock(string, DockNode, DockEdge, float, int)"/>
    /// says; false when that leaves the arrangement as it was.</summary>
    private bool DockPanel(string panel, DockNode target, DockEdge edge, float share, int index)
    {
        var source = GroupOf(panel);
        if (source != null && source == target && edge == DockEdge.Center)
        {
            Focused = panel;
            return Reorder(source, panel, index);
        }
        string? refill = null;
        if (source is { Panels.Count: 1 } && (source == target || source == Root))
        {
            refill = NewPanel?.Invoke();
            if (refill == null && source == target) return false;
            if (refill != null)
                source.Panels.Add(refill);
        }
        if (source != null)
        {
            Detach(panel, out var collapsed, out var replacement);
            if (collapsed == target)
                target = replacement!;
        }
        Focused = source == target && refill != null ? refill : panel;
        if (edge == DockEdge.Center && target is DockGroup group)
        {
            Insert(group, panel, index);
            group.Active = panel;
            if (WindowOf(group) is { } window)
                Raise(window);
            return true;
        }
        PlaceBeside(new DockGroup { Id = NextId++, Panels = { panel }, Active = panel }, target, edge, share);
        return true;
    }

    /// <summary>Docks everything in <paramref name="window"/> into or beside
    /// <paramref name="target"/>, outside the window, and removes the window.</summary>
    public void Dock(DockWindow window, DockNode target, DockEdge edge, float share = 0.5f, int index = -1)
    {
        if (Holds(window.Root, target) || !Windows.Remove(window)) return;
        _isMoving = false;
        Changes++;
        Focused = Groups(window.Root).First().Active ?? Focused;
        if (edge == DockEdge.Center && target is DockGroup group)
        {
            var first = true;
            foreach (var moved in PanelsIn(window.Root))
            {
                Insert(group, moved, index < 0 ? -1 : index++);
                if (first) group.Active = moved;
                first = false;
            }
            return;
        }
        PlaceBeside(window.Root, target, edge, share);
    }

    /// <summary>Takes <paramref name="panel"/> out of where it is into a new floating window at
    /// <paramref name="rect"/>, in front of the others.</summary>
    public DockWindow Float(string panel, UiRect rect)
    {
        Changes++;
        if (GroupOf(panel) is { Panels.Count: 1 } source && source == Root)
            Refill(source);
        Detach(panel, out _, out _);
        Focused = panel;
        var window = new DockWindow
        {
            Id = NextId++,
            Root = new DockGroup { Id = NextId++, Panels = { panel }, Active = panel },
            Rect = rect,
        };
        Windows.Add(window);
        return window;
    }

    /// <summary>Removes every panel <paramref name="keep"/> rejects — ones a saved layout names
    /// that the app no longer has — and every repeat of a panel after the first, so each shows in
    /// one place, without raising <see cref="PanelClosed"/>.</summary>
    public void Retain(Func<string, bool> keep)
    {
        var seen = new HashSet<string>();
        var removed = false;
        foreach (var group in AllGroups().ToList())
        {
            for (var i = 0; i < group.Panels.Count;)
            {
                var panel = group.Panels[i];
                if (keep(panel) && seen.Add(panel))
                {
                    i++;
                    continue;
                }
                Detach(group, i, out _, out _);
                removed = true;
            }
        }
        if (removed)
            Changes++;
    }

    /// <summary>Every group, docked then floating, in the order they are drawn.</summary>
    public IEnumerable<DockGroup> AllGroups() => Groups(Root).Concat(Windows.SelectMany(w => Groups(w.Root)));

    /// <summary>Whether any group, docked or floating, holds a panel. Allocates nothing, so it can
    /// run every frame.</summary>
    public bool HasPanels()
    {
        if (HasPanels(Root))
            return true;
        foreach (var window in Windows)
        {
            if (HasPanels(window.Root))
                return true;
        }
        return false;
    }

    private static bool HasPanels(DockNode node) => node switch
    {
        DockGroup group => group.Panels.Count > 0,
        DockSplit split => HasPanels(split.First) || HasPanels(split.Second),
        _ => false,
    };

    /// <summary>A layout arranged as this one is, with the same ids, as plain data: none of this one's
    /// callbacks, handlers or focus, and no node or list shared with it.</summary>
    public DockLayout CopyArrangement()
    {
        return new DockLayout
        {
            Root = CopyNode(Root),
            Windows = Windows.ConvertAll(CopyWindow),
            NextId = NextId,
        };
    }

    private static DockWindow CopyWindow(DockWindow window) => new()
    {
        Id = window.Id,
        Root = CopyNode(window.Root),
        X = window.X,
        Y = window.Y,
        Width = window.Width,
        Height = window.Height,
    };

    private static DockNode CopyNode(DockNode node) => node switch
    {
        DockSplit split => new DockSplit
        {
            Id = split.Id,
            Direction = split.Direction,
            Ratio = split.Ratio,
            First = CopyNode(split.First),
            Second = CopyNode(split.Second),
        },
        DockGroup group => new DockGroup { Id = group.Id, Panels = group.Panels.ToList(), Active = group.Active },
        _ => throw new ArgumentOutOfRangeException(nameof(node)),
    };

    /// <summary>Whether two of the nodes or windows of a whole layout have the same id. The dock space
    /// only numbers the parts that have none, and keeps a splitter's or tab strip's state by id, so two
    /// parts under one id would move together.</summary>
    public bool RepeatsAnId()
    {
        var seen = new HashSet<int>();
        if (RepeatsAnId(Root, seen))
            return true;
        foreach (var window in Windows)
        {
            if (window.Id != 0 && !seen.Add(window.Id))
                return true;
            if (RepeatsAnId(window.Root, seen))
                return true;
        }
        return false;
    }

    private static bool RepeatsAnId(DockNode node, HashSet<int> seen)
    {
        if (node.Id != 0 && !seen.Add(node.Id))
            return true;
        if (node is DockSplit split)
            return RepeatsAnId(split.First, seen) || RepeatsAnId(split.Second, seen);
        return false;
    }

    /// <summary>Every panel under <paramref name="node"/>, group by group.</summary>
    public static List<string> PanelsIn(DockNode node) => Groups(node).SelectMany(g => g.Panels).ToList();

    internal void Focus(string panel) => Focused = panel;

    internal void AssignIds()
    {
        var max = MaxId(Root);
        foreach (var window in Windows)
            max = Math.Max(max, Math.Max(window.Id, MaxId(window.Root)));
        if (NextId <= max)
            NextId = max + 1;
        FillIds(Root);
        foreach (var window in Windows)
        {
            if (window.Id == 0)
                window.Id = NextId++;
            FillIds(window.Root);
        }
    }

    private static int MaxId(DockNode node) =>
        node is DockSplit split ? Math.Max(node.Id, Math.Max(MaxId(split.First), MaxId(split.Second))) : node.Id;

    private void FillIds(DockNode node)
    {
        if (node.Id == 0)
            node.Id = NextId++;
        if (node is not DockSplit split) return;
        FillIds(split.First);
        FillIds(split.Second);
    }

    private bool Detach(string panel, out DockNode? collapsed, out DockNode? replacement)
    {
        collapsed = replacement = null;
        if (GroupOf(panel) is not { } group) return false;
        Detach(group, group.Panels.IndexOf(panel), out collapsed, out replacement);
        return true;
    }

    private void Detach(DockGroup group, int at, out DockNode? collapsed, out DockNode? replacement)
    {
        collapsed = replacement = null;
        var panel = group.Panels[at];
        group.Panels.RemoveAt(at);
        if (group.Active == panel)
            group.Active = group.Panels.Count == 0 ? null : group.Panels[Math.Min(at, group.Panels.Count - 1)];
        if (Focused == panel)
            Focused = group.Active;
        if (group.Panels.Count > 0 || Root == group) return;
        if (FindParent(group, out var window) is { } parent)
        {
            replacement = parent.First == group ? parent.Second : parent.First;
            collapsed = parent;
            Replace(parent, replacement);
        }
        else if (window != null)
        {
            Windows.Remove(window);
        }
    }

    private void Refill(DockGroup group)
    {
        if (NewPanel?.Invoke() is { } panel)
            group.Panels.Add(panel);
    }

    private void PlaceBeside(DockNode node, DockNode target, DockEdge edge, float share)
    {
        var before = edge is DockEdge.Left or DockEdge.Top;
        var split = new DockSplit
        {
            Id = NextId++,
            Direction = edge is DockEdge.Left or DockEdge.Right ? Direction.Horizontal : Direction.Vertical,
            Ratio = before ? share : 1f - share,
        };
        Replace(target, split);
        split.First = before ? node : target;
        split.Second = before ? target : node;
    }

    private static void Insert(DockGroup group, string panel, int index)
    {
        if (index < 0 || index > group.Panels.Count)
            group.Panels.Add(panel);
        else
            group.Panels.Insert(index, panel);
    }

    /// <summary>Moves <paramref name="panel"/> to <paramref name="index"/> in its group and brings it to
    /// the front; false when it was there and in front already.</summary>
    private static bool Reorder(DockGroup group, string panel, int index)
    {
        var from = group.Panels.IndexOf(panel);
        var wasActive = group.Active == panel;
        group.Active = panel;
        if (index < 0 || index > group.Panels.Count) index = group.Panels.Count;
        if (index == from || index == from + 1) return !wasActive;
        group.Panels.RemoveAt(from);
        group.Panels.Insert(index > from ? index - 1 : index, panel);
        return true;
    }

    private void Replace(DockNode old, DockNode replacement)
    {
        if (Root == old)
        {
            Root = replacement;
            return;
        }
        if (FindParent(old, out var window) is { } parent)
        {
            if (parent.First == old) parent.First = replacement;
            else parent.Second = replacement;
        }
        else if (window != null)
        {
            window.Root = replacement;
        }
    }

    private DockSplit? FindParent(DockNode node, out DockWindow? window)
    {
        window = null;
        if (FindParent(Root, node) is { } parent) return parent;
        foreach (var w in Windows)
        {
            if (w.Root == node)
            {
                window = w;
                return null;
            }
            if (FindParent(w.Root, node) is { } found)
            {
                window = w;
                return found;
            }
        }
        return null;
    }

    private static DockSplit? FindParent(DockNode root, DockNode node) => Nodes(root)
        .OfType<DockSplit>()
        .FirstOrDefault(split => split.First == node || split.Second == node);

    private static bool Holds(DockNode root, DockNode node) => Nodes(root).Contains(node);

    private static DockGroup? GroupOf(DockNode node, string panel) => node switch
    {
        DockGroup group => group.Panels.Contains(panel) ? group : null,
        DockSplit split => GroupOf(split.First, panel) ?? GroupOf(split.Second, panel),
        _ => null,
    };

    private static IEnumerable<DockGroup> Groups(DockNode node) => Nodes(node).OfType<DockGroup>();

    private static IEnumerable<DockNode> Nodes(DockNode node)
    {
        yield return node;
        if (node is not DockSplit split) yield break;
        foreach (var child in Nodes(split.First))
            yield return child;
        foreach (var child in Nodes(split.Second))
            yield return child;
    }
}
