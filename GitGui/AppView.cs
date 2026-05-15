using ZGF.Gui;
using ZGF.Gui.Layouts;

namespace GitGui;

public sealed class AppView : View
{
    public AppView()
    {
        Children.Add(new BorderLayoutView
        {
            West = new RepoBar(),
        });
        Children.Add(new OverlayView());
    }
}

public sealed class OverlayView : View
{
    private IMessageBus? _messageBus;

    private readonly RectView _background;
    private readonly AddRepoDialog _dialog;
    private bool _isOpen;

    public OverlayView()
    {
        _background = new RectView
        {
            BackgroundColor = 0,
        };
        _dialog = new AddRepoDialog(Close);
        Children.Add(_background);
    }

    protected override void OnAttachedToContext(Context context)
    {
        _messageBus = context.Get<IMessageBus>();
        _messageBus?.Subscribe<AddRepoMessage>(OnAddRepoMessageReceived);
    }

    protected override void OnDetachedFromContext(Context context)
    {
        _messageBus?.Unsubscribe<AddRepoMessage>(OnAddRepoMessageReceived);
        _messageBus = null;
    }

    private void OnAddRepoMessageReceived(AddRepoMessage obj)
    {
        Open();
    }

    private void Open()
    {
        if (_isOpen)
            return;
        _isOpen = true;
        _background.BackgroundColor = 0xB0000000;
        Children.Add(_dialog);
    }

    private void Close()
    {
        if (!_isOpen)
            return;
        _isOpen = false;
        _background.BackgroundColor = 0;
        Children.Remove(_dialog);
    }

    protected override void OnLayoutChildren()
    {
        var position = Position;
        _background.LeftConstraint = position.Left;
        _background.BottomConstraint = position.Bottom;
        _background.MinWidthConstraint = position.Width;
        _background.MaxWidthConstraint = position.Width;
        _background.MaxHeightConstraint = position.Height;
        _background.LayoutSelf();

        if (!_isOpen)
            return;

        const float dialogWidth = 360f;
        const float dialogHeight = 230f;
        _dialog.LeftConstraint = position.Left + (position.Width - dialogWidth) * 0.5f;
        _dialog.BottomConstraint = position.Bottom + (position.Height - dialogHeight) * 0.5f;
        _dialog.MinWidthConstraint = dialogWidth;
        _dialog.MaxWidthConstraint = dialogWidth;
        _dialog.MaxHeightConstraint = dialogHeight;
        _dialog.LayoutSelf();
    }
}

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