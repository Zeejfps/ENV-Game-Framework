using ZGF.Desktop.Input;

namespace ZGF.Gui.Desktop.Input;

/// <summary>
/// An update to the in-flight IME composition. Carries text the user is composing but has not
/// committed, so a handler must display it without letting it reach the value it is editing —
/// committed text arrives on <see cref="TextInputEvent"/> as it always has.
/// <para>
/// An empty <see cref="Preedit"/> ends the composition. It cannot distinguish commit from cancel:
/// on commit the text follows on the character path, on cancel nothing does. So the only correct
/// response to it is to drop the preedit, never to insert it.
/// </para>
/// </summary>
public struct CompositionEvent : IEvent
{
    public required PreeditText Preedit { get; init; }
    public required EventPhase Phase { get; set; }
    public bool IsConsumed { get; private set; }

    public void Consume()
    {
        IsConsumed = true;
    }
}
