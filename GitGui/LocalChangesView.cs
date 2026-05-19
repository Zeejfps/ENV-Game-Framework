using ZGF.Gui;
using ZGF.Gui.Layouts;
using ZGF.Observable;

namespace GitGui;

public sealed class LocalChangesView : MultiChildView
{
    public LocalChangesView()
    {
        var content = new LocalChangesContentView();
        var commitBar = new CommitBarView();

        AddChildToSelf(new RectView
        {
            BackgroundColor = CommitsPalette.Background,
            Children =
            {
                new BorderLayoutView
                {
                    Center = content,
                    South = commitBar,
                },
            },
        });

        this.UsePresenter(ctx =>
        {
            var vm = new LocalChangesViewModel(
                ctx.Require<IRepoRegistry>(),
                ctx.Require<IGitService>(),
                ctx.Require<IUiDispatcher>(),
                ctx.Require<IMessageBus>());
            content.Bind(vm);
            commitBar.Bind(vm);
            return vm;
        });
    }
}
