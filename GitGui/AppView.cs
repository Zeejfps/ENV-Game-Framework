using ZGF.Gui;
using ZGF.Gui.Layouts;

namespace GitGui;

public sealed class AppView : MultiChildView
{
    public AppView(IRepoRegistry registry)
    {
        Children.Add(new BorderLayoutView
        {
            // RepoBar minWidth matches RepoBar.RowTextAvailableWidth's design width (220 px)
            // since the row-name truncation is computed once at attach time against that
            // constant. Until truncation is recomputed on resize, allow growing but not
            // shrinking — otherwise long repo names would overflow the bar.
            West = ResizableLeftSidebar.Build(new RepoBar(registry), initialWidth: 220f, minWidth: 220f),
            Center = new BorderLayoutView
            {
                North = new ActionsToolbar(),
                Center = new BorderLayoutView
                {
                    West = ResizableLeftSidebar.Build(new BranchesView(), initialWidth: 220f),
                    Center = new MainContentView(),
                },
            },
        });
        Children.Add(new DragOverlay());
        Children.Add(new OverlayView());
    }
}
