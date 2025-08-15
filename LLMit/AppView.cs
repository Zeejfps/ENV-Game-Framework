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
            BackgroundColor = 0x212121
        };
        
        AddChildToSelf(background);
        AddChildToSelf(new StartNewChatView());
    }
}

public sealed class StartNewChatView : View
{
    public StartNewChatView()
    {
        var textInput = new TextInput
        {
            PreferredHeight = 40,
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
                            TextColor = 0xFFFFFF
                        },
                        textInput
                    }
                }
            }
        };
        
        var textInputController = new TextInputDefaultKbmController(textInput);
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
            BackgroundColor = 0x181818,
        };
        
        AddChildToSelf(background);
    }
}