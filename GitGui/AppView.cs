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

internal static class DialogPalette
{
    public const uint Background = 0xFF1E1F22;
    public const uint Border = 0xFF313338;
    public const uint TitleText = 0xFFE6E6E6;
    public const uint BodyText = 0xFFDCDDDE;

    public const uint ButtonNormal = 0xFF2B2D31;
    public const uint ButtonHover = 0xFF3A3D43;
    public const uint ButtonBorder = 0xFF3E4047;
    public const uint ButtonBorderHover = 0xFF5865F2;

    public const uint CloseNormal = 0x00000000;
    public const uint CloseHover = 0xFF3A3D43;
    public const uint CloseTextNormal = 0xFFB5B9C0;
    public const uint CloseTextHover = 0xFFFFFFFF;
}

public sealed class AddRepoDialog : View
{
    public AddRepoDialog(Action onClose)
    {
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
                    Gap = 20,
                    CrossAxisAlignment = CrossAxisAlignment.Stretch,
                    Children =
                    {
                        new FlexRowView
                        {
                            MainAxisAlignment = MainAxisAlignment.SpaceBetween,
                            CrossAxisAlignment = CrossAxisAlignment.Center,
                            PreferredHeight = 28,
                            Children =
                            {
                                new TextView
                                {
                                    Text = "Add Repository",
                                    TextColor = DialogPalette.TitleText,
                                    VerticalTextAlignment = TextAlignment.Center,
                                },
                                new DialogCloseButton(onClose),
                            }
                        },
                        new FlexColumnView
                        {
                            Gap = 8,
                            CrossAxisAlignment = CrossAxisAlignment.Stretch,
                            Children =
                            {
                                new DialogButton("Init New", () => { /* TODO */ })
                                {
                                    PreferredHeight = 40,
                                },
                                new DialogButton("Clone", () => { /* TODO */ })
                                {
                                    PreferredHeight = 40,
                                },
                                new DialogButton("Open", () => { /* TODO */ })
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

public sealed class DialogCloseButton : View
{
    public DialogCloseButton(Action onClick)
    {
        PreferredWidth = 28;
        PreferredHeight = 28;

        var label = new TextView
        {
            Text = "X",
            TextColor = DialogPalette.CloseTextNormal,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
        };
        var background = new RectView
        {
            BackgroundColor = DialogPalette.CloseNormal,
            BorderRadius = BorderRadiusStyle.All(4),
            Children = { label }
        };

        AddChildToSelf(background);
        Behaviors.Add(new HoverableButtonController(
            onClick,
            isHovered =>
            {
                background.BackgroundColor = isHovered ? DialogPalette.CloseHover : DialogPalette.CloseNormal;
                label.TextColor = isHovered ? DialogPalette.CloseTextHover : DialogPalette.CloseTextNormal;
            }));
    }
}

public sealed class DialogButton : View
{
    public DialogButton(string label, Action onClick)
    {
        var background = new RectView
        {
            BackgroundColor = DialogPalette.ButtonNormal,
            BorderColor = BorderColorStyle.All(DialogPalette.ButtonBorder),
            BorderSize = BorderSizeStyle.All(1),
            BorderRadius = BorderRadiusStyle.All(6),
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
        };
        AddChildToSelf(background);
        Behaviors.Add(new HoverableButtonController(
            onClick,
            isHovered =>
            {
                background.BackgroundColor = isHovered ? DialogPalette.ButtonHover : DialogPalette.ButtonNormal;
                background.BorderColor = BorderColorStyle.All(
                    isHovered ? DialogPalette.ButtonBorderHover : DialogPalette.ButtonBorder);
            }));
    }
}

public sealed class HoverableButtonController(Action onClick, Action<bool> onHoverChanged) : KeyboardMouseController
{
    public override void OnMouseEnter(ref MouseEnterEvent e)
    {
        onHoverChanged(true);
    }

    public override void OnMouseExit(ref MouseExitEvent e)
    {
        onHoverChanged(false);
    }

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