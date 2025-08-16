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

        _layout = new RowView();

        var bg = new RectView
        {
            BackgroundColor = 0xFF1C1C1C,
            Children =
            {
                _layout
            }
        };

        AddChildToSelf(bg);
    }
}