using ZGF.Gui;
using ZGF.Gui.Layouts;

namespace LLMit.Views;

public sealed class TabBarView : View
{
    public TabBarView()
    {
        PreferredHeight = 40;

        var bg = new RectView
        {
            BackgroundColor = 0xFF1C1C1C
        };

        var layout = new RowView
        {
            Children =
            {
                new TabView
                {
                    IsHighlighted = true,
                }
            }
        };

        AddChildToSelf(bg);
        AddChildToSelf(layout);
    }
}