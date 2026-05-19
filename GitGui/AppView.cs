using ZGF.Gui;
using ZGF.Gui.Layouts;

namespace GitGui;

public sealed class AppView : MultiChildView
{
    public AppView(TooltipSurfaceView tooltipSurfaceView)
    {
        // North-stack holds the toolbar plus an in-progress-op banner the presenter
        // inserts/removes as the active repo's state changes. Keeping it as a column
        // (rather than nesting another BorderLayout's North) means the banner spans
        // both sidebars and shows above both History and LocalChanges views.
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
                North = northStack,
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

        Children.Add(tooltipSurfaceView);

        Behaviors.Add(new DialogPresenter(dialogSurfaceView));
        Behaviors.Add(new OperationStateBannerPresenter(operationBanner));
    }
}