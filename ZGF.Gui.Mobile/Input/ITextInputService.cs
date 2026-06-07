namespace ZGF.Gui.Mobile.Input;

/// <summary>
/// Platform service that drives the system keyboard, registered in the <see cref="Context"/> by
/// each host (the iOS app backs it with a hidden UITextField). A control calls
/// <see cref="BeginEdit"/> when tapped to raise the keyboard and route its keystrokes; the
/// touch-input parallel is <see cref="MobileInputSystem"/>.
/// </summary>
public interface ITextInputService
{
    /// <summary>Raise the keyboard and start routing edits to <paramref name="client"/>.</summary>
    void BeginEdit(ITextInputClient client);

    /// <summary>Dismiss the keyboard if <paramref name="client"/> is the one being edited.</summary>
    void EndEdit(ITextInputClient client);
}
