namespace GitGui;

/// <summary>
/// Typo-safe modifier identifiers. Modifiers describe <em>effective</em> states, not raw events
/// — the call site composes the effective state (e.g. <c>IsEnabled &amp;&amp; IsHovered</c>) before
/// toggling.
/// </summary>
public static class ModifierNames
{
    public const string Hovered = "hovered";
    public const string Active = "active";
    public const string Pressed = "pressed";
    public const string Selected = "selected";
    public const string Disabled = "disabled";
    public const string Focused = "focused";
    public const string Open = "open";
    public const string Busy = "busy";
    public const string Dragging = "dragging";

    public const string Detached = "detached";
}
