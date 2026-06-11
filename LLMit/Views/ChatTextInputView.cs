using ZGF.Gui;
using ZGF.Gui.Desktop;
using ZGF.Gui.Desktop.Components.TextInput;
using ZGF.Gui.Desktop.Controllers;
using ZGF.Gui.Desktop.Input;
using ZGF.Gui.Views;
using ZGF.KeyboardModule;

namespace LLMit.Views;

public sealed class ChatTextInputView : MultiChildView
{
    public Action<ReadOnlySpan<char>>? Submit { get; set; }

    private readonly TextInputView _textInput;

    public ChatTextInputView(Context context)
    {
        _textInput = new TextInputView(context.Canvas)
        {
            Width = 500,
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

        var inputSystem = context.Require<InputSystem>();
        _textInput.UseController(inputSystem, () => new ChatTextInputViewController(_textInput, inputSystem, context.Get<IClipboard>())
        {
            IsMultiLine = true,
            Submit = OnSubmit
        });
    }

    private void OnSubmit(ReadOnlySpan<char> text)
    {
        Submit?.Invoke(text);
    }

    public void Clear()
    {
        _textInput.Clear();
    }
}

public sealed class ChatTextInputViewController : BaseTextInputKbmController, IDisposable
{
    public Action<ReadOnlySpan<char>>? Submit { get; set; }

    private readonly TextInputView _textInput;

    public ChatTextInputViewController(TextInputView textInput, InputSystem inputSystem, IClipboard? clipboard = null)
        : base(textInput, inputSystem, clipboard)
    {
        _textInput = textInput;
    }

    public void Dispose()
    {
        Submit = null;
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