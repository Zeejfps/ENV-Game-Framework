using LLMit.ViewModels;
using ZGF.Gui;
using ZGF.Gui.Components;
using ZGF.Gui.Desktop;
using ZGF.Gui.Desktop.Components.TextInput;
using ZGF.Gui.Desktop.Controllers;
using ZGF.Gui.Desktop.Input;
using ZGF.Gui.Views;
using ZGF.KeyboardModule;

namespace LLMit.Components;

public sealed record ChatTextInput : Primitive
{
    protected override View CreateView(Context ctx)
    {
        var vm = ctx.Require<AppViewModel>();
        var inputSystem = ctx.Require<InputSystem>();

        var textInput = new TextInputView(ctx.Canvas)
        {
            Width = 500,
            TextWrap = TextWrap.Wrap,
            TextColor = 0xFFA6A6A6,
            CaretColor = 0xFFA6A6A6,
            SelectionRectColor = 0xAA466583,
        };

        textInput.UseController(inputSystem, () => new ChatTextInputController(textInput, inputSystem, ctx.Get<IClipboard>())
        {
            IsMultiLine = true,
            Submit = text =>
            {
                vm.StartNewChat(text.ToString());
                textInput.Clear();
            },
        });

        return new RectView
        {
            Padding = PaddingStyle.All(10),
            BackgroundColor = 0xFF303030,
            Children =
            {
                textInput
            },
        };
    }
}

public sealed class ChatTextInputController : BaseTextInputKbmController, IDisposable
{
    public Action<ReadOnlySpan<char>>? Submit { get; set; }

    private readonly TextInputView _textInput;

    public ChatTextInputController(TextInputView textInput, InputSystem inputSystem, IClipboard? clipboard = null)
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
