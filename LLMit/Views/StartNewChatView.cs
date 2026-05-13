using ZGF.Gui;
using ZGF.Gui.Layouts;
using ZGF.Gui.Tests;

namespace LLMit.Views;

public delegate void StartNewChatCallback(string? model, ReadOnlySpan<char> text);

public sealed class StartNewChatView : View
{
    public StartNewChatCallback? StartNewChatCallback { get; set; }

    private readonly ChatTextInputView _chatTextInput;
    private readonly ModelSelector _modelSelector;

    public StartNewChatView()
    {
        _chatTextInput = new ChatTextInputView
        {
            Submit = OnSubmit
        };

        _modelSelector = new ModelSelector
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
                                        new TextView
                                        {
                                            Text = "What would you like to ask",
                                            TextColor = 0xFFFFFFFF,
                                            VerticalTextAlignment = TextAlignment.Center,
                                        },
                                        _modelSelector,
                                        new TextView
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
    }

    protected override void OnAttachedToContext(Context context)
    {
        base.OnAttachedToContext(context);
        var contextMenuManager = context.Get<ContextMenuManager>();
        System.Diagnostics.Debug.Assert(contextMenuManager != null);
        context.Get<InputSystem>()!.RegisterController(_modelSelector, new ModelSelectorController(_modelSelector, contextMenuManager));
    }

    private void OnSubmit(ReadOnlySpan<char> text)
    {
        _chatTextInput.Clear();
        StartNewChatCallback?.Invoke(_modelSelector.Model, text);
    }
}