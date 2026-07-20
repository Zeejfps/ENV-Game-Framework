using ZGF.KeyboardModule;

namespace ZGF.Gui.Desktop.Input;

public struct KeyboardKeyEvent : IEvent
{
    public required KeyboardKey Key { get; init; }
    public required InputState State { get; init; }
    public required InputModifiers Modifiers { get; init; }
    public required EventPhase Phase { get; set; }
    public KeyClaim Claim { get; private set; }

    public bool IsConsumed => Claim != KeyClaim.None;

    /// <summary>Handled as a command. Propagation stops and this keystroke produces no text.</summary>
    public void Consume()
    {
        Claim = KeyClaim.Command;
    }

    /// <summary>Claimed by a text target: propagation stops, but the characters the OS derives from
    /// this key are still delivered. For keys that type, and for the keys an IME has taken — the
    /// Enter that picks a candidate must not reach the app, yet it is what commits the text.</summary>
    public void ConsumeAsText()
    {
        Claim = KeyClaim.Text;
    }
}