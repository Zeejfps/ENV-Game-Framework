using System.Text;

namespace ZGF.Gui.Desktop.Input;

/// <summary>
/// A character committed by the OS text-input pipeline, already resolved for keyboard layout,
/// modifiers and dead keys. This is the only event that inserts text.
/// <see cref="KeyboardKeyEvent"/> carries physical key positions (layout-independent), so it can
/// drive shortcuts, navigation and editing gestures but must never be decoded into characters —
/// doing so hard-codes a US layout and makes non-ASCII scripts untypable.
///
/// <para>It arrives on its own OS callback but is caused by that key event, not a peer of it: a key
/// taken as a <see cref="KeyClaim.Command"/> produces no text at all. Not every character has a key
/// behind it though — an IME commits without one — so absence of a claim means deliver.</para>
/// </summary>
public struct TextInputEvent : IEvent
{
    public required Rune Rune { get; init; }
    public required EventPhase Phase { get; set; }
    public bool IsConsumed { get; private set; }

    public void Consume()
    {
        IsConsumed = true;
    }
}
