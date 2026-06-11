using ZGF.Gui;
using ZGF.Gui.Desktop;
using ZGF.Gui.Desktop.Controllers;
using ZGF.Gui.Desktop.Input;
using ZGF.Gui.Views;

namespace LLMit.Views;

public sealed class AppView : MultiChildView
{
    public AppView(Context context)
    {
        var layout = new BorderLayoutView
        {
            West = new LeftSideBarView(context.Canvas),
            Center = new CenterArea(context),
        };

        AddChildToSelf(layout);   
    }
}

public sealed class CenterArea : MultiChildView
{
    private readonly TabView _newChatTabView;
    private readonly TabBarView _tabBarView;
    private readonly MultiChildView _tabContentsView;
    private readonly ICanvas _canvas;
    private readonly InputSystem _inputSystem;

    public CenterArea(Context context)
    {
        _canvas = context.Canvas;
        _inputSystem = context.Require<InputSystem>();

        _newChatTabView = new TabView(_canvas)
        {
            Text = "New Chat",
            IsActive = true,
        };
        _newChatTabView.UseController(_inputSystem, () => new TabViewController(_newChatTabView));

        _tabBarView = new TabBarView
        {
            Children =
            {
                _newChatTabView
            }
        };

        var background = new RectView
        {
            BackgroundColor = 0xFF212121
        };

        _tabContentsView = new MultiChildView
        {
            Children =
            {
                new StartNewChatView(context)
                {
                    StartNewChatCallback = StartNewChat,
                },
            }
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
                    _tabContentsView
                }
            }
        };

        AddChildToSelf(background);
        AddChildToSelf(layout);
    }

    private void StartNewChat(string? model, ReadOnlySpan<char> text)
    {
        _newChatTabView.IsActive = false;
        var tabView = new TabView(_canvas)
        {
            Text = model,
            IsActive = true,
        };
        tabView.UseController(_inputSystem, () => new TabViewController(tabView));
        _tabBarView.Children.Add(tabView);
        _tabContentsView.Children.Clear();
    }
}

public sealed class TabViewController : KeyboardMouseController
{
    private readonly TabView _tabView;

    public TabViewController(TabView tabView)
    {
        _tabView = tabView;
    }

    public override void OnMouseEnter(ref MouseEnterEvent e)
    {
        _tabView.IsHighlighted = true;
    }

    public override void OnMouseExit(ref MouseExitEvent e)
    {
        _tabView.IsHighlighted = false;
    }
}