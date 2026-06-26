namespace ZGF.Gui;

/// <summary>
/// A widget's semantic role — the kind of thing it is, independent of how it's painted. A
/// struct-wrapped string (not an enum) so the well-known roles are discoverable constants yet a
/// downstream app can mint its own without editing the framework. The <see cref="Name"/> follows
/// ARIA conventions ("button", "listitem") so snapshot output reads like an accessibility tree.
/// </summary>
public readonly record struct AccessibilityRole(string Name)
{
    public static readonly AccessibilityRole None = new("");
    public static readonly AccessibilityRole Button = new("button");
    public static readonly AccessibilityRole Text = new("text");
    public static readonly AccessibilityRole ListItem = new("listitem");
    public static readonly AccessibilityRole List = new("list");
    public static readonly AccessibilityRole Checkbox = new("checkbox");
    public static readonly AccessibilityRole TextBox = new("textbox");
    public static readonly AccessibilityRole Image = new("image");

    public override string ToString() => Name;
}

/// <summary>
/// Self-owned interaction states a widget knows about itself and sets reactively. Focus and hover
/// are deliberately absent: the view isn't their source of truth — the input system is — so a
/// reader layers those in separately.
/// </summary>
[Flags]
public enum AccessibilityStates
{
    None = 0,
    Disabled = 1 << 0,
    Selected = 1 << 1,
    Checked = 1 << 2,
    Expanded = 1 << 3,
    Busy = 1 << 4,
}

/// <summary>
/// Accessibility metadata for a <see cref="View"/>: its role, an accessible label, and self-owned
/// states. One value type rather than scattered properties, so it carries no per-field allocation
/// and updates with a <c>with</c> expression. Render- and layout-inert — assistive tech and the
/// test harness read it; nothing about measure, layout, or paint depends on it.
/// </summary>
public readonly record struct AccessibilityInfo(
    AccessibilityRole Role = default,
    string? Label = null,
    AccessibilityStates States = AccessibilityStates.None)
{
    public bool IsEmpty => Role == default && Label is null && States == AccessibilityStates.None;

    /// <summary>Overlays <paramref name="over"/> onto this: each field from <paramref name="over"/>
    /// wins when set, else this view's intrinsic value is kept. Lets a widget set an intrinsic role
    /// while an author overrides just the label.</summary>
    public AccessibilityInfo Overlay(AccessibilityInfo over) => new(
        over.Role == default ? Role : over.Role,
        over.Label ?? Label,
        over.States == AccessibilityStates.None ? States : over.States);
}
