using ZGF.Gui;
using ZGF.Gui.Layouts;

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

        this.UseViewModel<LocalChangesViewModel>(vm =>
        {
            content.Bind(vm);
            commitBar.Bind(vm);
        });
    }
}
