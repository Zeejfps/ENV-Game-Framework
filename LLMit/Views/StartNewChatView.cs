using ZGF.Gui;
using ZGF.Gui.Desktop.Controllers;
using ZGF.Gui.Desktop.Input;
using ZGF.Gui.Views;

namespace LLMit.Views;

public delegate void StartNewChatCallback(string? model, ReadOnlySpan<char> text);

public sealed class StartNewChatView : MultiChildView
{
    public StartNewChatCallback? StartNewChatCallback { get; set; }

    private readonly ChatTextInputView _chatTextInput;
    private readonly ModelSelector _modelSelector;

    public StartNewChatView(Context context)
    {
        _chatTextInput = new ChatTextInputView(context)
        {
            Submit = OnSubmit
        };

        _modelSelector = new ModelSelector(context.Canvas)
        {

        };

        var layout = new CenterView
        {
            Children =
            {
                new ColumnView
                {
                    Gap = 10,
                    Children =
                    {
                        new CenterView
                        {
                            Children =
                            {
                                new RowView
                                {
                                    Gap = 5,
                                    Children =
                                    {
                                        new TextView(context.Canvas)
                                        {
                                            Text = "What would you like to ask",
                                            TextColor = 0xFFFFFFFF,
                                            VerticalTextAlignment = TextAlignment.Center,
                                        },
                                        _modelSelector,
                                        new TextView(context.Canvas)
                                        {
                                            Text = "?",
                                            TextColor = 0xFFFFFFFF,
                                            VerticalTextAlignment = TextAlignment.Center,
                                        }
                                    }
                                }
                            }
                        },
                        _chatTextInput,
                    }
                }
            }
        };

        AddChildToSelf(layout);

        _modelSelector.UseController(context.Require<InputSystem>(), () => new ModelSelectorController(_modelSelector, context));
    }

    private void OnSubmit(ReadOnlySpan<char> text)
    {
        _chatTextInput.Clear();
        StartNewChatCallback?.Invoke(_modelSelector.Model, text);
    }
}