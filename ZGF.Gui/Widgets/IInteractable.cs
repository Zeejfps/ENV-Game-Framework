using ZGF.Observable;

namespace ZGF.Gui.Widgets;

/// <summary>
/// The interaction-state surface an input controller drives, decoupled from any widget. A
/// controller reports hover and press by writing the states and reads <see cref="Enabled"/> to
/// know whether to; a widget binds its visuals to the same states and reads the rising edge of
/// <see cref="Pressed"/> as activation. Lives on a per-widget state object (a view model), so the
/// controller binds to <em>state</em> rather than to a widget — the same <see cref="KbmController"/>
/// drives a checkbox, a button, or anything else that exposes this surface, and a touch controller
/// later would drive the same one.
/// </summary>
public interface IInteractable
{
    /// <summary>True while a pointer is over the widget; the controller writes it.</summary>
    IWritable<bool> Hovered { get; }

    /// <summary>True while the widget is held down; its rising edge is an activation. The controller writes it.</summary>
    IWritable<bool> Pressed { get; }

    /// <summary>When false the controller ignores hover and press — the widget is inert. Read-only to the controller.</summary>
    IReadable<bool> Enabled { get; }
}
