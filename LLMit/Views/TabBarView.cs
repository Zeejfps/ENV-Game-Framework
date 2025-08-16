using ZGF.Gui;
using ZGF.Gui.Layouts;

namespace LLMit.Views;

public sealed class TabBarView : View
{
    private readonly RowView _layout;

    public override IComponentCollection Children => _layout.Children;

    public TabBarView()
    {
        PreferredHeight = 40;

        var bg = new RectView
        {
            BackgroundColor = 0xFF1C1C1C
        };

        _layout = new RowView
        {
            Children =
            {
                new TabView
                {
                    IsActive = true,
                }
            }
        };

        AddChildToSelf(bg);
        AddChildToSelf(_layout);
    }
}