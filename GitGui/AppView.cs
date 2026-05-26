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
                West = ResizableLeftSidebar.Build(
                    new FlexColumnView
                    {
                        CrossAxisAlignment = CrossAxisAlignment.Stretch,
                        Children =
                        {
                            new BranchesHeader(),
                            new FlexItem { Grow = 1, Child = new BranchesView() },
                        },
                    },
                    initialWidth: 220f),
                Center = new BorderLayoutView
                {
                    North = new FlexColumnView
                    {
                        CrossAxisAlignment = CrossAxisAlignment.Stretch,
                        Children =
                        {
                            new OperationBannerView(),
                            new ActionsToolbar(),
                        },
                    },
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
