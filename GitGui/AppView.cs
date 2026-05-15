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
    public const uint Separator = 0xFF2A2C30;
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

public sealed class DialogCloseButton : View
{
    public DialogCloseButton(Action onClick)
    {
        PreferredWidth = 28;
        PreferredHeight = 28;

        var label = new TextView
        {
            Text = "×",
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
    private readonly FlexColumnView _column;
    private readonly AddRepoButton _addButton = new();
    private IMessageBus? _bus;
    private IRepoRegistry? _registry;

    public RepoBar()
    {
        PreferredWidth = 72;
        _column = new FlexColumnView
        {
            Gap = 8,
            CrossAxisAlignment = CrossAxisAlignment.Stretch,
        };
        AddChildToSelf(new RectView
        {
            BackgroundColor = DialogPalette.Background,
            BorderColor = new BorderColorStyle { Right = DialogPalette.Border },
            BorderSize = new BorderSizeStyle { Right = 1 },
            Padding = PaddingStyle.All(10),
            Children = { _column }
        });
    }

    protected override void OnAttachedToContext(Context context)
    {
        _bus = context.Get<IMessageBus>();
        _registry = context.Get<IRepoRegistry>();
        _bus?.Subscribe<ReposChangedMessage>(OnReposChanged);
        _bus?.Subscribe<ActiveRepoChangedMessage>(OnActiveRepoChanged);
        Rebuild();
    }

    protected override void OnDetachedFromContext(Context context)
    {
        _bus?.Unsubscribe<ReposChangedMessage>(OnReposChanged);
        _bus?.Unsubscribe<ActiveRepoChangedMessage>(OnActiveRepoChanged);
        _bus = null;
        _registry = null;
    }

    private void OnReposChanged(ReposChangedMessage _) => Rebuild();
    private void OnActiveRepoChanged(ActiveRepoChangedMessage _) => Rebuild();

    private void Rebuild()
    {
        _column.Children.Clear();
        _column.Children.Add(_addButton);
        if (_registry is null) return;
        var activeId = _registry.Active?.Id;
        foreach (var repo in _registry.Repos)
        {
            _column.Children.Add(new RepoButton(repo, isActive: repo.Id == activeId));
        }
    }
}

public sealed class AddRepoButton : View
{
    public AddRepoButton()
    {
        PreferredHeight = 52;

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
                    Text = "+",
                    TextColor = DialogPalette.TitleText,
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center,
                }
            }
        };
        AddChildToSelf(background);
        Behaviors.Add(new HoverableButtonController(
            () => Context?.Get<IMessageBus>()?.Broadcast<AddRepoMessage>(),
            isHovered =>
            {
                background.BackgroundColor = isHovered ? DialogPalette.ButtonHover : DialogPalette.ButtonNormal;
                background.BorderColor = BorderColorStyle.All(
                    isHovered ? DialogPalette.ButtonBorderHover : DialogPalette.ButtonBorder);
            }));
    }
}

public sealed class RepoButton : View
{
    public RepoButton(Repo repo, bool isActive)
    {
        PreferredHeight = 52;

        var letter = string.IsNullOrEmpty(repo.DisplayName)
            ? "?"
            : char.ToUpperInvariant(repo.DisplayName[0]).ToString();

        var normalBorder = repo.IsMissing ? 0x80313338u : DialogPalette.ButtonBorder;
        var activeBorder = DialogPalette.ButtonBorderHover;
        var textColor = repo.IsMissing ? 0x80E6E6E6u : DialogPalette.TitleText;

        var background = new RectView
        {
            BackgroundColor = DialogPalette.ButtonNormal,
            BorderRadius = BorderRadiusStyle.All(6),
            Children =
            {
                new TextView
                {
                    Text = letter,
                    TextColor = textColor,
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center,
                }
            }
        };
        ApplyBorder(background, isActive ? activeBorder : normalBorder, isActive);
        AddChildToSelf(background);

        Behaviors.Add(new HoverableButtonController(
            () => Context?.Get<IRepoRegistry>()?.SetActive(repo.Id),
            isHovered =>
            {
                background.BackgroundColor = isHovered ? DialogPalette.ButtonHover : DialogPalette.ButtonNormal;
                if (!isActive)
                    ApplyBorder(background, isHovered ? activeBorder : normalBorder, isActive: false);
            }));
    }

    private static void ApplyBorder(RectView rect, uint color, bool isActive)
    {
        if (isActive)
        {
            rect.BorderColor = new BorderColorStyle
            {
                Left = DialogPalette.ButtonBorderHover,
                Right = DialogPalette.ButtonBorder,
                Top = DialogPalette.ButtonBorder,
                Bottom = DialogPalette.ButtonBorder,
            };
            rect.BorderSize = new BorderSizeStyle { Left = 3, Right = 1, Top = 1, Bottom = 1 };
        }
        else
        {
            rect.BorderColor = BorderColorStyle.All(color);
            rect.BorderSize = BorderSizeStyle.All(1);
        }
    }
}

public record struct AddRepoMessage;