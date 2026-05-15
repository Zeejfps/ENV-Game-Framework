using ZGF.Gui;
using ZGF.Gui.Layouts;

namespace GitGui;

public sealed class AddRepoDialog : View
{
    private const float CloseButtonSize = 28f;

    public AddRepoDialog(Action onClose)
    {
        var title = new TextView
        {
            Text = "Add Repository",
            TextColor = DialogPalette.TitleText,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
        };

        var headerRow = new FlexRowView
        {
            CrossAxisAlignment = CrossAxisAlignment.Center,
            PreferredHeight = 28,
            Children =
            {
                new View { PreferredWidth = CloseButtonSize },
                title,
                new DialogCloseButton(onClose),
            }
        };
        headerRow.UpdateStyle(title, new FlexStyle { Grow = 1 });

        AddChildToSelf(new RectView
        {
            BackgroundColor = DialogPalette.Background,
            BorderColor = BorderColorStyle.All(DialogPalette.Border),
            BorderSize = BorderSizeStyle.All(1),
            BorderRadius = BorderRadiusStyle.All(10),
            Padding = PaddingStyle.All(20),
            Children =
            {
                new FlexColumnView
                {
                    Gap = 14,
                    CrossAxisAlignment = CrossAxisAlignment.Stretch,
                    Children =
                    {
                        headerRow,
                        new RectView
                        {
                            BackgroundColor = DialogPalette.Separator,
                            PreferredHeight = 1,
                        },
                        new FlexColumnView
                        {
                            Gap = 8,
                            CrossAxisAlignment = CrossAxisAlignment.Stretch,
                            Children =
                            {
                                new DialogButton("Clone", () => { /* TODO */ })
                                {
                                    PreferredHeight = 40,
                                },
                                new DialogButton("Open", () =>
                                {
                                    var picker = Context?.Get<IFolderPicker>();
                                    var path = picker?.PickFolder("Open Repository");
                                    if (string.IsNullOrEmpty(path)) return;
                                    Context?.Get<IRepoRegistry>()?.Open(path);
                                    onClose();
                                })
                                {
                                    PreferredHeight = 40,
                                },
                                new DialogButton("New", () => { /* TODO */ })
                                {
                                    PreferredHeight = 40,
                                },
                            }
                        },
                    }
                }
            }
        });
    }
}