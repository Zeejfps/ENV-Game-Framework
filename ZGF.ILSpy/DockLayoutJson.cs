using System.Diagnostics;
using System.Text.Json.Serialization;
using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;

namespace ZGF.ILSpy;

/// <summary>A dock layout as a settings file can hold it. A part the file leaves out is the part a new
/// layout starts with; a part it holds as null, or a panel it names as null, is missing, and a layout
/// missing a part is none. The parts have setters, not init: the source-generated reader gives an init
/// property the file leaves out its type's default, not the one it starts with here.</summary>
internal sealed class DockLayoutJson
{
    public DockNodeJson? Root { get; set; } = new DockGroupJson();
    public List<DockWindowJson?>? Windows { get; set; } = new();
    public int NextId { get; set; } = 1;

    public static DockLayoutJson From(DockLayout layout) => new()
    {
        Root = DockNodeJson.From(layout.Root),
        Windows = layout.Windows.Select(static DockWindowJson? (w) => DockWindowJson.From(w)).ToList(),
        NextId = layout.NextId,
    };

    /// <summary>The layout, or null when it is missing a part.</summary>
    public DockLayout? Parse()
    {
        var root = Root?.Parse();
        if (root == null || Windows == null)
            return null;
        var windows = new List<DockWindow>(Windows.Count);
        foreach (var window in Windows)
        {
            var parsed = window?.Parse();
            if (parsed == null)
                return null;
            windows.Add(parsed);
        }
        return new DockLayout { Root = root, Windows = windows, NextId = NextId };
    }
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(DockSplitJson), "split")]
[JsonDerivedType(typeof(DockGroupJson), "group")]
internal abstract class DockNodeJson
{
    public int Id { get; set; }

    public static DockNodeJson From(DockNode node) => node switch
    {
        DockSplit split => DockSplitJson.From(split),
        DockGroup group => DockGroupJson.From(group),
        _ => throw new UnreachableException(),
    };

    /// <summary>The node, or null when it is missing a part.</summary>
    public abstract DockNode? Parse();
}

internal sealed class DockSplitJson : DockNodeJson
{
    public Direction Direction { get; set; }
    public float Ratio { get; set; } = 0.5f;
    public DockNodeJson? First { get; set; } = new DockGroupJson();
    public DockNodeJson? Second { get; set; } = new DockGroupJson();

    public static DockSplitJson From(DockSplit split) => new()
    {
        Id = split.Id,
        Direction = split.Direction,
        Ratio = split.Ratio,
        First = DockNodeJson.From(split.First),
        Second = DockNodeJson.From(split.Second),
    };

    public override DockNode? Parse()
    {
        var first = First?.Parse();
        if (first == null)
            return null;
        var second = Second?.Parse();
        if (second == null)
            return null;
        return new DockSplit { Id = Id, Direction = Direction, Ratio = Ratio, First = first, Second = second };
    }
}

internal sealed class DockGroupJson : DockNodeJson
{
    public List<string?>? Panels { get; set; } = new();
    public string? Active { get; set; }

    public static DockGroupJson From(DockGroup group) => new() { Id = group.Id, Panels = new List<string?>(group.Panels), Active = group.Active };

    public override DockNode? Parse()
    {
        if (Panels == null)
            return null;
        var panels = Panels.OfType<string>().ToList();
        if (panels.Count != Panels.Count)
            return null;
        return new DockGroup { Id = Id, Panels = panels, Active = Active };
    }
}

internal sealed class DockWindowJson
{
    public int Id { get; set; }
    public DockNodeJson? Root { get; set; } = new DockGroupJson();
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }

    public static DockWindowJson From(DockWindow window) => new()
    {
        Id = window.Id,
        Root = DockNodeJson.From(window.Root),
        X = window.X,
        Y = window.Y,
        Width = window.Width,
        Height = window.Height,
    };

    public DockWindow? Parse()
    {
        var root = Root?.Parse();
        if (root == null)
            return null;
        return new DockWindow { Id = Id, Root = root, X = X, Y = Y, Width = Width, Height = Height };
    }
}
