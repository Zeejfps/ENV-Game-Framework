using ZGF.Gui;
using ZGF.Gui.Layouts;
using ZGF.Gui.Tests;

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
    public AddRepoDialog(Action onClose)
    {
        AddChildToSelf(new RectView
        {
            BackgroundColor = 0xFF2A2D31,
            BorderColor = BorderColorStyle.All(0xFF3D4147),
            BorderSize = BorderSizeStyle.All(1),
            Padding = PaddingStyle.All(16),
            Children =
            {
                new FlexColumnView
                {
                    Gap = 12,
                    MainAxisAlignment = MainAxisAlignment.SpaceEvenly,
                    CrossAxisAlignment = CrossAxisAlignment.Stretch,
                    Children =
                    {
                        new FlexRowView
                        {
                            MainAxisAlignment = MainAxisAlignment.SpaceBetween,
                            CrossAxisAlignment = CrossAxisAlignment.Center,
                            PreferredHeight = 24,
                            Children =
                            {
                                new TextView
                                {
                                    Text = "Add Repository",
                                    TextColor = 0xFFFFFFFF,
                                    VerticalTextAlignment = TextAlignment.Center,
                                },
                                new DialogCloseButton(onClose),
                            }
                        },
                        new DialogButton("Init New", () => { /* TODO */ })
                        {
                            PreferredHeight = 36,
                        },
                        new DialogButton("Clone", () => { /* TODO */ })
                        {
                            PreferredHeight = 36,
                        },
                        new DialogButton("Open", () => { /* TODO */ })
                        {
                            PreferredHeight = 36,
                        },
                    }
                }
            }
        });
    }
}

public sealed class DialogCloseButton : View
{
    public DialogCloseButton(Action onClick)
    {
        PreferredWidth = 24;
        PreferredHeight = 24;
        AddChildToSelf(new RectView
        {
            BackgroundColor = 0,
            Children =
            {
                new TextView
                {
                    Text = "X",
                    TextColor = 0xFFB5B9C0,
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center,
                }
            }
        });
        Behaviors.Add(new DialogButtonController(onClick));
    }
}

public sealed class DialogButton : View
{
    public DialogButton(string label, Action onClick)
    {
        AddChildToSelf(new RectView
        {
            BackgroundColor = 0xFF3D4147,
            Children =
            {
                new TextView
                {
                    Text = label,
                    TextColor = 0xFFFFFFFF,
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center,
                }
            }
        });
        Behaviors.Add(new DialogButtonController(onClick));
    }
}

public sealed class DialogButtonController(Action onClick) : KeyboardMouseController
{
    public override void OnMouseButtonStateChanged(ref MouseButtonEvent e)
    {
        if (e.Button == MouseButton.Left && e.State == InputState.Pressed)
        {
            onClick();
            e.Consume();
        }
    }
}

public sealed class RepoBar : View
{
    public RepoBar()
    {
        PreferredWidth = 80;
        AddChildToSelf(new RectView
        {
            BackgroundColor = 0xFF0000FF,
            Padding = PaddingStyle.All(5),
            Children =
            {
                new ColumnView
                {
                    Children =
                    {
                        new AddRepoButton(),
                    }
                }
            }
        });
    }
}

public sealed class AddRepoButton : View
{
    public AddRepoButton()
    {
        PreferredHeight = 70;
        Children.Add(new RectView
        {
            BackgroundColor = 0xFFFF22FF,
        });
        Behaviors.Add(new AddRepoButtonController());
    }
}

public sealed class AddRepoButtonController : KeyboardMouseController
{
    public override void OnMouseButtonStateChanged(ref MouseButtonEvent e)
    {
        if (e.Button == MouseButton.Left && e.State == InputState.Pressed)
        {
            Context?.Get<IMessageBus>()?.Broadcast<AddRepoMessage>();
            e.Consume();
        }
    }
}

public record struct AddRepoMessage;