namespace ZGF.Gui.Mobile.Input;

/// <summary>
/// A view that can be edited via the system keyboard. The host raises the keyboard and forwards
/// raw key events here — <see cref="InsertText"/> / <see cref="DeleteBackward"/>, the same surface
/// iOS's UIKeyInput exposes — so the client owns its own text buffer and decides what the text
/// means (parse it as a number, validate it, etc.).
/// </summary>
public interface ITextInputClient
{
    /// <summary>Which on-screen keyboard to present.</summary>
    TextInputKeyboard Keyboard { get; }

    /// <summary>Whether the client currently holds any text (drives the keyboard's delete state).</summary>
    bool HasText { get; }

    /// <summary>Append typed text (usually a single character).</summary>
    void InsertText(string text);

    /// <summary>Delete the character before the caret.</summary>
    void DeleteBackward();

    /// <summary>Called once when editing ends (keyboard dismissed or focus moved away).</summary>
    void OnEditingEnded();
}
