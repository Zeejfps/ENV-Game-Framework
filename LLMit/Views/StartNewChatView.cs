using ZGF.Gui;
using ZGF.Gui.Layouts;
using ZGF.Gui.Tests;

namespace LLMit.Views;

public sealed class StartNewChatView : View
{
    private readonly ChatTextInputView _chatTextInput;

    public StartNewChatView()
    {
        _chatTextInput = new ChatTextInputView
        {
            Submit = OnSubmit
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
                                        new ModelSelector(),
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

    private void OnSubmit(ReadOnlySpan<char> text)
    {
        _chatTextInput.Clear();
        Console.WriteLine("Start new chat");
    }
}