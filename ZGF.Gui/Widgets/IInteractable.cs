using ZGF.Observable;

namespace ZGF.Gui.Widgets;

/// <summary>
/// The interaction-state surface a widget exposes so an input controller can drive it without the
/// widget owning any input code. A controller reports hover and press by writing the states; the
/// widget binds its visuals to them and reads the rising edge of <see cref="Pressed"/> as activation.
/// Modality-neutral — a desktop <c>KbmController</c> or a touch one later drives the same surface.
/// </summary>
public interface IInteractable
{
    /// <summary>True while a pointer is over the widget.</summary>
    State<bool> Hovered { get; }

    /// <summary>True while the widget is held down; its rising edge is an activation.</summary>
    State<bool> Pressed { get; }

    /// <summary>When false the controller ignores hover and press — the widget is inert.</summary>
    State<bool> Enabled { get; }
}
