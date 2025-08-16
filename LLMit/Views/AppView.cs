using ZGF.Gui;
using ZGF.Gui.Layouts;

namespace LLMit.Views;

public sealed class AppView : View
{
    public AppView()
    {
        var layout = new BorderLayoutView
        {
            West = new LeftSideBarView(),
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

        var layout = new BorderLayoutView
        {
            North = new TabBarView(),
            Center = new RectView
            {
                BorderSize = new BorderSizeStyle
                {
                    Top = 1,
                    Left = 1,
                },
                BorderColor = BorderColorStyle.All(0xFF4f4f4f),
                Children =
                {
                    new StartNewChatView(),
                }
            }
        };

        AddChildToSelf(background);
        AddChildToSelf(layout);
    }
}