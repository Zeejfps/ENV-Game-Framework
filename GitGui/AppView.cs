using ZGF.Gui;
using ZGF.Gui.Layouts;

namespace GitGui;

public sealed class AppView : MultiChildView
{
    public AppView()
    {
        Children.Add(new BorderLayoutView
        {
            West = ResizableLeftSidebar.Build(new RepoBar(), initialWidth: 220f, minWidth: 220f),
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

        var dialogSurfaceView = new DialogSurfaceView();
        Children.Add(dialogSurfaceView);
        
        Behaviors.Add(new DialogPresenter(dialogSurfaceView));
    }
}