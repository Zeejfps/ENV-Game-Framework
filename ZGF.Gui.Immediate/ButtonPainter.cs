namespace ZGF.Gui.Immediate;

/// <summary>How a button is emphasized: <see cref="Neutral"/> on the control fill with an outline,
/// <see cref="Primary"/> filled with the accent for the action a dialog or screen exists for,
/// <see cref="Ghost"/> with no fill until the pointer is over it.</summary>
public enum ButtonVariant
{
    Neutral,
    Primary,
    Ghost,
}

/// <summary>Whether a button takes input: <see cref="Busy"/> while the work it started is under way,
/// which it shows without being greyed out as a <see cref="Disabled"/> one is.</summary>
public enum ButtonAvailability
{
    Enabled,
    Disabled,
    Busy,
}

/// <summary>What a button's behavior found this frame, for its painter to draw. The pointer and
/// focus fields are false unless it is <see cref="ButtonAvailability.Enabled"/>.</summary>
/// <param name="Id">The id the painter records the button's element with, which makes it hit-testable.</param>
/// <param name="Hovered">The pointer is over it.</param>
/// <param name="Down">It is drawn pushed in: held by the pointer, or activated by a key this frame.</param>
/// <param name="FocusVisible">It holds keyboard focus while <see cref="Ui.FocusVisible"/>, so it draws a focus ring.</param>
/// <param name="Activated">It was clicked, or activated with Enter or Space, this frame.</param>
public readonly record struct ButtonState(int Id, ButtonAvailability Availability, bool Hovered, bool Down, bool FocusVisible, bool Activated);

/// <summary>What a text button shows: <see cref="Label"/>, led by <see cref="Icon"/> when there is
/// one, at <see cref="Width"/>, or fitted to its label when null.</summary>
public readonly record struct ButtonFace(string Label, ButtonVariant Variant = ButtonVariant.Neutral, UiIcon? Icon = null, Sizing? Width = null);

/// <summary>What a square icon button shows.</summary>
public readonly record struct IconButtonFace(IconGlyph Glyph, ButtonVariant Variant = ButtonVariant.Ghost);

/// <summary>Records a text button: its element, hit-testable as <see cref="ButtonState.Id"/>, and
/// everything drawn in it.</summary>
public delegate void ButtonPainter(Ui ui, in ButtonState state, in ButtonFace face);

/// <summary>Records an icon button as a <see cref="ButtonPainter"/> records a text button.</summary>
public delegate void IconButtonPainter(Ui ui, in ButtonState state, in IconButtonFace face);
