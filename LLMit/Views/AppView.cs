using ZGF.Gui;
using ZGF.Gui.Layouts;
using ZGF.Gui.Tests;

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
        var tabView = new TabView
        {
            Text = model,
            IsActive = false,
        };
        tabView.Controller = new TabViewController(tabView);
        _tabBarView.Children.Add(tabView);
    }
}

public sealed class TabViewController : KeyboardMouseController
{
    private readonly TabView _tabView;

    public TabViewController(TabView tabView)
    {
        _tabView = tabView;
    }

    public override View View => _tabView;

    public override void OnMouseEnter(ref MouseEnterEvent e)
    {
        _tabView.IsHighlighted = true;
    }

    public override void OnMouseExit(ref MouseExitEvent e)
    {
        _tabView.IsHighlighted = false;
    }
}