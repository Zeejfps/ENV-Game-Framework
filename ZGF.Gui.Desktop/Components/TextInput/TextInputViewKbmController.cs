namespace ZGF.Gui.Desktop.Components.TextInput;

using ZGF.Gui.Desktop.Input;

public sealed class TextInputViewKbmController : BaseTextInputKbmController
{
    public TextInputViewKbmController(TextInputView textInput, InputSystem inputSystem, IClipboard? clipboard = null)
        : base(textInput, inputSystem, clipboard)
    {
    }
}