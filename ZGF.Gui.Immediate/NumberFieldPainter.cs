namespace ZGF.Gui.Immediate;

/// <summary>A part of a number field the pointer can be on: the value in its middle, or the arrow at either end that
/// steps it.</summary>
public enum NumberFieldPart
{
    None,
    Value,
    Decrease,
    Increase,
}

/// <summary>What a number field's behavior found this frame, for its painter to draw while the value is not being
/// typed (while it is, the field is a stock text field). The pointer and focus fields are false unless it is
/// <see cref="Enabled"/>.</summary>
/// <param name="Id">The id the painter records the field's element with, which makes it hit-testable.</param>
/// <param name="Text">The value as it shows.</param>
/// <param name="Hovered">The pointer is over the field; its arrows show.</param>
/// <param name="HoveredPart">The part under the pointer.</param>
/// <param name="PressedPart">The part held by the pointer, drawn pushed in.</param>
/// <param name="Scrubbing">The value is being dragged sideways.</param>
/// <param name="FocusVisible">It holds keyboard focus while <see cref="Ui.FocusVisible"/>, so it draws a focus ring.</param>
/// <param name="AtMin">The value is its least, so the decrease arrow does nothing.</param>
/// <param name="AtMax">The value is its most, so the increase arrow does nothing.</param>
public readonly record struct NumberFieldState(int Id, string Text, bool Enabled, bool Hovered, NumberFieldPart HoveredPart,
    NumberFieldPart PressedPart, bool Scrubbing, bool FocusVisible, bool AtMin, bool AtMax);

/// <summary>What a number field shows besides its value: its <see cref="Width"/>, and the room each arrow at its ends
/// takes, <see cref="ArrowWidth"/>, which its behavior hit-tests against.</summary>
public readonly record struct NumberFieldFace(float Width, float ArrowWidth);

/// <summary>Records a number field that is not being typed in: its element, hit-testable as
/// <see cref="NumberFieldState.Id"/>, and everything drawn in it.</summary>
public delegate void NumberFieldPainter(Ui ui, in NumberFieldState state, in NumberFieldFace face);
