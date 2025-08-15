using ZGF.Gui;
using ZGF.Gui.Layouts;

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