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
        group.Active = panel;
        Focused = panel;
        if (WindowOf(group) is { } window)
            BringToFront(window);
    }

    public void BringToFront(DockWindow window)
    {
        if (Windows.Count == 0 || Windows[^1] == window || !Windows.Remove(window)) return;
        Windows.Add(window);
    }

    /// <summary>Removes <paramref name="panel"/>, collapsing what it leaves empty, and raises
    /// <see cref="PanelClosed"/>.</summary>
    public void Close(string panel)
    {
        if (!Detach(panel, out _, out _)) return;
        PanelClosed?.Invoke(panel);
    }

    /// <summary>Moves <paramref name="panel"/>, or adds it when not shown, into or beside
    /// <paramref name="target"/>. A new split gives the panel <paramref name="share"/> of the space;
    /// a tab goes at <paramref name="index"/>, or last.</summary>
    public void Dock(string panel, DockNode target, DockEdge edge, float share = 0.5f, int index = -1)
    {
        var source = GroupOf(panel);
        if (source != null && source == target && edge == DockEdge.Center)
        {
            Reorder(source, panel, index);
            Focused = panel;
            return;
        }
        string? refill = null;
        if (source is { Panels.Count: 1 } && (source == target || source == Root))
        {
            refill = NewPanel?.Invoke();
            if (refill == null && source == target) return;
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
                BringToFront(window);
            return;
        }
        PlaceBeside(new DockGroup { Id = NextId++, Panels = { panel }, Active = panel }, target, edge, share);
    }

    /// <summary>Docks everything in <paramref name="window"/> into or beside
    /// <paramref name="target"/>, outside the window, and removes the window.</summary>
    public void Dock(DockWindow window, DockNode target, DockEdge edge, float share = 0.5f, int index = -1)
    {
        if (Holds(window.Root, target) || !Windows.Remove(window)) return;
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
    /// that the app no longer has — without raising <see cref="PanelClosed"/>.</summary>
    public void Retain(Func<string, bool> keep)
    {
        var drop = AllGroups().SelectMany(g => g.Panels).Where(p => !keep(p)).ToList();
        foreach (var panel in drop)
            Detach(panel, out _, out _);
    }

    /// <summary>Every group, docked then floating, in the order they are drawn.</summary>
    public IEnumerable<DockGroup> AllGroups() => Groups(Root).Concat(Windows.SelectMany(w => Groups(w.Root)));

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
        var at = group.Panels.IndexOf(panel);
        group.Panels.RemoveAt(at);
        if (group.Active == panel)
            group.Active = group.Panels.Count == 0 ? null : group.Panels[Math.Min(at, group.Panels.Count - 1)];
        if (Focused == panel)
            Focused = group.Active;
        if (group.Panels.Count > 0) return true;

        if (Root == group) return true;
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
        return true;
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

    private static void Reorder(DockGroup group, string panel, int index)
    {
        var from = group.Panels.IndexOf(panel);
        group.Active = panel;
        if (index < 0 || index > group.Panels.Count) index = group.Panels.Count;
        if (index == from || index == from + 1) return;
        group.Panels.RemoveAt(from);
        group.Panels.Insert(index > from ? index - 1 : index, panel);
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

    private static DockSplit? FindParent(DockNode root, DockNode node)
    {
        if (root is not DockSplit split) return null;
        if (split.First == node || split.Second == node) return split;
        return FindParent(split.First, node) ?? FindParent(split.Second, node);
    }

    private static bool Holds(DockNode root, DockNode node) =>
        root == node || (root is DockSplit split && (Holds(split.First, node) || Holds(split.Second, node)));

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
