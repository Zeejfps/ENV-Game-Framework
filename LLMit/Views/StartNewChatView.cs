using ZGF.Gui;
using ZGF.Gui.Layouts;
using ZGF.Gui.Tests;

namespace LLMit.Views;

public sealed class StartNewChatView : View
{
    public StartNewChatView()
    {
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
                                            //HorizontalTextAlignment = TextAlignment.Center,
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
                        new ChatTextInputView
                        {
                            Submit = OnSubmit
                        },
                    }
                }
            }
        };

        AddChildToSelf(layout);
    }

    private void OnSubmit(ReadOnlySpan<char> text)
    {
        Console.WriteLine("Start new chat");
    }
}