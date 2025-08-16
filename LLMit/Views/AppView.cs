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
    private readonly TabBarView _tabBarView;

    public CenterArea()
    {
        _tabBarView = new TabBarView();

        var background = new RectView
        {
            BackgroundColor = 0xFF212121
        };

        var layout = new BorderLayoutView
        {
            North = _tabBarView,
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
                    new StartNewChatView
                    {
                        StartNewChatCallback = StartNewChat,
                    },
                }
            }
        };

        AddChildToSelf(background);
        AddChildToSelf(layout);
    }

    private void StartNewChat(string? model, ReadOnlySpan<char> text)
    {
        _tabBarView.Children.Add(new TabView());
    }
}