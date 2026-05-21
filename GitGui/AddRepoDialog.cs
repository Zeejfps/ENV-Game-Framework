using ZGF.Gui;
using ZGF.Gui.Layouts;

namespace GitGui;

public sealed class AddRepoDialog : MultiChildView
{
    public AddRepoDialog(Action onClose)
    {
        PreferredWidth = 360;
        PreferredHeight = 230;

        AddChildToSelf(DialogFrame.Build("Add Repository", onClose, new FlexColumnView
        {
            Gap = 14,
            CrossAxisAlignment = CrossAxisAlignment.Stretch,
            Children =
            {
                new FlexColumnView
                {
                    Gap = 8,
                    CrossAxisAlignment = CrossAxisAlignment.Stretch,
                    Children =
                    {
                        new DialogButton("Clone", () => { /* TODO */ }) { PreferredHeight = 40 },
                        new DialogButton("Open", () =>
                        {
                            var shell = Context?.Get<IPlatformShell>();
                            var path = shell?.PickFolder("Open Repository");
                            if (string.IsNullOrEmpty(path)) return;
                            Context?.Get<IRepoRegistry>()?.Open(path);
                            onClose();
                        }) { PreferredHeight = 40 },
                        new DialogButton("New", () => { /* TODO */ }) { PreferredHeight = 40 },
                    }
                },
            }
        }));
    }
}