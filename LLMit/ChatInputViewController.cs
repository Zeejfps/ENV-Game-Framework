using ZGF.Gui;
using ZGF.Gui.Tests;
using ZGF.KeyboardModule;

namespace LLMit;

public sealed class ChatInputViewController : BaseTextInputKbmController
{
    public ChatInputViewController(TextInputView textInput) : base(textInput)
    {
    }

    protected override void OnKeyboardKeyPressed(ref KeyboardKeyEvent e)
    {
        if (e.Modifiers == InputModifiers.Shift && e.Key == KeyboardKey.Enter)
        {
            Enter('\n');
            e.Consume();
            return;
        }
        
        base.OnKeyboardKeyPressed(ref e);
    }
}