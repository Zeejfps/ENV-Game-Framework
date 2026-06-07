namespace ZGF.Gui.Mobile.Input;

/// <summary>
/// The on-screen keyboard a text field wants. Platform-neutral; each host maps it to its native
/// keyboard type (iOS UIKeyboardType, Android InputType).
/// </summary>
public enum TextInputKeyboard
{
    Default,
    Number,
    Decimal,
}
