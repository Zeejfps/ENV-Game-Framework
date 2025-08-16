using ZGF.Gui;
using ZGF.Gui.Tests;

namespace LLMit.Views;

public sealed class ChatTextInputView : View
{
    public ChatTextInputView()
    {
        var textInput = new TextInputView
        {
            PreferredWidth = 500,
            TextWrap = TextWrap.Wrap,
            TextColor = 0xFFA6A6A6,
            CaretColor = 0xFFA6A6A6,
            SelectionRectColor = 0xAA466583,
        };

        var bg = new RectView
        {
            Padding = PaddingStyle.All(10),
            BackgroundColor = 0xFF303030,
            Children =
            {
                textInput
            }
        };

        var textInputController = new TextInputViewKbmController(textInput)
        {
            IsMultiLine = true
        };
        textInput.Controller = textInputController;

        AddChildToSelf(bg);
    }
}