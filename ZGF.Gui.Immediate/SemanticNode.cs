using ZGF.Geometry;

namespace ZGF.Gui.Immediate;

/// <summary>What a <see cref="SemanticNode"/> is to the person using it.</summary>
public enum SemanticRole
{
    /// <summary>An element with an id or a path and nothing more said about it.</summary>
    Group,
    Text,
    Button,
    Checkbox,
    Switch,
    TextField,
    TextArea,
    Slider,
    /// <summary>How far something has got; its value is the fraction done, 0 to 1.</summary>
    ProgressBar,
    SegmentedControl,
    Segment,
    TabList,
    Tab,
    MenuBar,
    Menu,
    MenuItem,
    List,
    ListItem,
    Tree,
    TreeItem,
    Dialog,
    /// <summary>The main window's content: the root of every snapshot.</summary>
    Window,
    /// <summary>A <see cref="Ui.Popup"/> no widget said more about.</summary>
    Popup,
    /// <summary>A <see cref="Ui.Window"/>: a window of its own over the main one.</summary>
    SubWindow,
    /// <summary>A <see cref="Ui.Modal"/>'s backdrop, covering the main window.</summary>
    Modal,
}

[Flags]
public enum SemanticStates
{
    None = 0,
    Disabled = 1 << 0,
    Focused = 1 << 1,
    Hovered = 1 << 2,
    Pressed = 1 << 3,
    Checked = 1 << 4,
    Mixed = 1 << 5,
    Selected = 1 << 6,
    Expanded = 1 << 7,
    Collapsed = 1 << 8,
    Busy = 1 << 9,
}

/// <summary>How much of a node the frame shows.</summary>
public enum Visibility
{
    Visible,
    /// <summary>Partly cut off by a clipping element around it.</summary>
    Clipped,
    /// <summary>Wholly outside a scroll around it: scrolling would show it.</summary>
    ScrolledOut,
    /// <summary>Not shown at all: of no size, faded out, or cut off by a clip that is no scroll.</summary>
    Hidden,
}

public enum SurfaceKind
{
    /// <summary>The main window.</summary>
    Main,
    /// <summary>A <see cref="Ui.Popup"/> shown in a window of its own.</summary>
    Popup,
    /// <summary>A <see cref="Ui.Window"/> shown in a window of its own.</summary>
    Window,
}

/// <summary>The window a node draws on: the main one, index 0, or a popup or window of its own,
/// whose top-left is <see cref="Rect"/>'s, in the main window's UI points, at
/// <see cref="DpiScale"/> pixels per point. Two are equal only when they are the same window of the same
/// <see cref="Ui"/>: a window replaced by another at the same place is another surface.</summary>
public readonly record struct SemanticSurface(SurfaceKind Kind, int Index, UiRect Rect, float DpiScale)
{
    /// <summary>The surface's identity within <see cref="Owner"/>, given when its canvas first drew as one and
    /// never given to another.</summary>
    internal long Id { get; init; }

    /// <summary>The <see cref="Ui"/> whose snapshot it came from.</summary>
    internal object? Owner { get; init; }
}

/// <summary>One element of a completed frame as a test sees it: what it is, what it says, its state,
/// and where it is, in the main window's UI points from its top-left.</summary>
public sealed class SemanticNode
{
    internal SemanticNode()
    {
    }

    /// <summary>The string ids and keys it was recorded under; null when it was given none.</summary>
    public TestPath? Path { get; internal init; }
    public SemanticRole Role { get; internal init; }
    public string? Label { get; internal set; }
    public string? Value { get; internal init; }
    public SemanticStates States { get; internal init; }
    public SemanticSurface Surface { get; internal init; }
    public UiRect Bounds { get; internal init; }
    public Visibility Visibility { get; internal init; }
    public IReadOnlyList<SemanticNode> Children => ChildList;
    public SemanticNode? Parent { get; internal set; }

    internal List<SemanticNode> ChildList { get; } = new();

    /// <summary>The part of <see cref="Bounds"/> its clips leave showing.</summary>
    internal UiRect VisibleBounds { get; init; }

    /// <summary>The id the pointer reaches it by: its element's own, or the nearest around it that has one.</summary>
    internal int HitId { get; init; }

    /// <summary>Whether it is in something that takes no pointer, such as a tooltip.</summary>
    internal bool PassThrough { get; init; }

    /// <summary>The input layer it was recorded in, counted in recording order; -1 for none.</summary>
    internal int Layer { get; init; }

    public bool Has(SemanticStates states) => (States & states) == states;

    public IEnumerable<SemanticNode> SelfAndDescendants()
    {
        yield return this;
        foreach (var child in ChildList)
        {
            foreach (var node in child.SelfAndDescendants())
                yield return node;
        }
    }

    public bool IsWithin(SemanticNode ancestor)
    {
        for (var node = this; node != null; node = node.Parent)
        {
            if (node == ancestor)
                return true;
        }
        return false;
    }

    /// <summary>This node alone, as one line of <see cref="SemanticSnapshot.ToText"/> without its indent.</summary>
    public override string ToString() => SemanticText.Line(this);
}
