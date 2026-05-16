using ZGF.Gui;
using ZGF.Gui.Layouts;

namespace GitGui;

public sealed class AppView : MultiChildView
{
    public AppView(IRepoRegistry registry)
    {
        Children.Add(new BorderLayoutView
        {
            West = new RepoBar(registry),
            Center = new CommitsPanelView(),
        });
        Children.Add(new DragOverlay());
        Children.Add(new OverlayView());
    }
}
