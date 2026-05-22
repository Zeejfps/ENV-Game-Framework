using ZGF.Gui;
using ZGF.Gui.Layouts;

namespace GitGui;

public sealed class AppView : MultiChildView
{
    public AppView()
    {
        // North-stack holds the toolbar plus an in-progress-op banner the presenter
        // inserts/removes as the active repo's state changes. It sits above
        // MainContentView only, horizontally aligned with the BranchesHeader to its left.
        var northStack = new FlexColumnView
        {
            CrossAxisAlignment = CrossAxisAlignment.Stretch,
            Children = { new ActionsToolbar() },
        };
        var operationBanner = new OperationStateBanner(northStack);

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
                    North = northStack,
                    Center = new MainContentView(),
                },
            },
        });
        Children.Add(new DragOverlay());

        var dialogSurfaceView = new DialogSurfaceView();
        Children.Add(dialogSurfaceView);

        Behaviors.Add(new DialogPresenter(dialogSurfaceView));
        Behaviors.Add(new OperationStateBannerPresenter(operationBanner));
    }
}