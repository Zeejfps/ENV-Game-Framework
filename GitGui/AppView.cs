using ZGF.Gui;
using ZGF.Gui.Layouts;

namespace GitGui;

public sealed class AppView : MultiChildView
{
    public AppView(PreferencesService preferences)
    {
        var prefs = preferences.Current;
        Children.Add(new BorderLayoutView
        {
            West = ResizableLeftSidebar.Build(
                new RepoBar(),
                initialWidth: prefs.RepoBarWidth,
                minWidth: 220f,
                onWidthChanged: preferences.SetRepoBarWidth),
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
                    initialWidth: prefs.BranchesWidth,
                    onWidthChanged: preferences.SetBranchesWidth),
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
