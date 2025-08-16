using ZGF.Gui;
using ZGF.Gui.Tests;
using ZGF.KeyboardModule;

namespace LLMit.Views;

public sealed class ChatTextInputView : View
{
    public Action<ReadOnlySpan<char>>? Submit { get; set; }

    private readonly TextInputView _textInput;

    public ChatTextInputView()
    {
        _textInput = new TextInputView
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
                _textInput
            }
        };

        AddChildToSelf(bg);
    }

    protected override void OnAttachedToContext(Context context)
    {
        base.OnAttachedToContext(context);
        _textInput.Controller = new ChatTextInputViewController(_textInput)
        {
            IsMultiLine = true,
            Submit = OnSubmit
        };
    }

    private void OnSubmit(ReadOnlySpan<char> text)
    {
        Submit?.Invoke(text);
    }
}

public sealed class ChatTextInputViewController : BaseTextInputKbmController
{
    public Action<ReadOnlySpan<char>>? Submit { get; set; }

    private readonly TextInputView _textInput;

    public ChatTextInputViewController(TextInputView textInput) : base(textInput)
    {
        _textInput = textInput;
    }

    public override void OnDisabled(Context context)
    {
        Submit = null;
        base.OnDisabled(context);
    }

    protected override void OnKeyboardKeyPressed(ref KeyboardKeyEvent e)
    {
        if (e.Modifiers == InputModifiers.None && e.Key == KeyboardKey.Enter)
        {
            e.Consume();
            Submit?.Invoke(_textInput.Text);
            return;
        }

        base.OnKeyboardKeyPressed(ref e);
    }
}