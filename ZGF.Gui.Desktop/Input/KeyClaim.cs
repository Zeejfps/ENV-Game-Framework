namespace ZGF.Gui.Desktop.Input;

/// <summary>
/// What a handler decided a key press was. Stopping propagation and suppressing the character the
/// OS derives from the key are separate outcomes: a text field claims the keys it is typing so they
/// never reach the app's shortcuts, yet the characters they produce must still arrive.
/// </summary>
public enum KeyClaim
{
    /// <summary>Nobody handled the key. It keeps propagating and still produces text.</summary>
    None = 0,

    /// <summary>Handled as a shortcut or gesture. Propagation stops and the key produces no text.</summary>
    Command,

    /// <summary>Claimed by whatever is editing. Propagation stops; the text still arrives.</summary>
    Text,
}
