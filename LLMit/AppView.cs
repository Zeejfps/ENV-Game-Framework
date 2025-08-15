using ZGF.Gui;
using ZGF.Gui.Layouts;
using ZGF.Gui.Tests;

namespace LLMit;

public sealed class AppView : View
{
    public AppView()
    {
        var layout = new BorderLayoutView
        {
            West = new LeftSideBar(),
            Center = new CenterArea(),
        };

        AddChildToSelf(layout);   
    }
}

public sealed class CenterArea : View
{
    public CenterArea()
    {
        var background = new RectView
        {
            BackgroundColor = 0xFF212121
        };
        
        AddChildToSelf(background);
        AddChildToSelf(new StartNewChatView());
    }
}

public sealed class StartNewChatView : View
{
    public StartNewChatView()
    {
        var textInput = new TextInputView
        {
            PreferredWidth = 500,
            IsMultiLine = true,
            TextWrap = TextWrap.Wrap,
            TextColor = 0xFFA6A6A6,
            CaretColor = 0xFFA6A6A6,
            SelectionRectColor = 0xAA466583,
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
                        new TextView
                        {
                            Text = "What would you like to ask?",
                            TextColor = 0xFFFFFFFF,
                            HorizontalTextAlignment = TextAlignment.Center,
                        },
                        new RectView
                        {
                            Padding = PaddingStyle.All(10),
                            BackgroundColor = 0xFF303030,
                            Children =
                            {
                                textInput
                            }
                        }
                    }
                }
            }
        };
        
        var textInputController = new ChatInputViewController(textInput);
        textInput.Controller = textInputController;
        
        AddChildToSelf(layout);
    }
}

public sealed class LeftSideBar : View
{
    public LeftSideBar()
    {
        PreferredWidth = 300;
        
        var background = new RectView
        {
            BackgroundColor = 0xFF181818,
        };
        
        AddChildToSelf(background);
    }
}