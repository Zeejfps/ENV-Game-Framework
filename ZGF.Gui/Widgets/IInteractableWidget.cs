using ZGF.Observable;

namespace ZGF.Gui.Widgets;

/// <summary>
/// A widget that is its own interaction target: it builds visuals and exposes the interaction-state
/// surface an input controller drives without the widget owning any input code. A controller reports
/// hover and press by writing the states; the widget binds its visuals to them and reads the rising
/// edge of <see cref="Pressed"/> as activation. Modality-neutral — a desktop <c>KbmController</c> or a
/// touch one later drives the same surface. Widget and target collapse into one reference at the
/// attach site, so the consumer writes <c>checkbox.WithController&lt;KbmController&gt;()</c> with no
/// separate target to pass.
/// </summary>
public interface IInteractableWidget : IWidget
{
    /// <summary>True while a pointer is over the widget; the controller writes it.</summary>
    IWritable<bool> Hovered { get; }

    /// <summary>True while the widget is held down; its rising edge is an activation. The controller writes it.</summary>
    IWritable<bool> Pressed { get; }

    /// <summary>When false the controller ignores hover and press — the widget is inert. Read-only to the controller.</summary>
    IReadable<bool> Enabled { get; }
}
