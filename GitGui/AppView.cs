using ZGF.Gui;
using ZGF.Gui.Layouts;

namespace GitGui;

public sealed class AppView : MultiChildView
{
    public AppView()
    {
        Children.Add(new BorderLayoutView
        {
            West = new RepoBar(),
            Center = new CommitsView(),
        });
        Children.Add(new OverlayView());
    }
}