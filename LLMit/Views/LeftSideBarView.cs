using ZGF.Gui;

namespace LLMit.Views;

public sealed class LeftSideBarView : View
{
    public LeftSideBarView()
    {
        PreferredWidth = 300;

        var background = new RectView
        {
            BackgroundColor = 0xFF181818,
        };

        AddChildToSelf(background);
    }
}