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

    public const uint RowTransparent = 0x00000000;
    public const uint RowHover = 0xFF2B2D31;
    public const uint RowActive = 0xFF404C8C;
    public const uint RowText = 0xFFB5B9C0;
    public const uint RowTextActive = 0xFFFFFFFF;
    public const uint RowTextMissing = 0x80B5B9C0;
    public const uint SectionHeaderText = 0xFF96989D;
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

public sealed class RepoBar : View
{
    public const int BarWidth = 220;
    private const int HorizontalPadding = 8;
    private const int RowTextIndent = 24;
    private const int RowTextRightPadding = 12;

    public static int RowTextAvailableWidth =>
        BarWidth - 2 * HorizontalPadding - RowTextIndent - RowTextRightPadding;

    private readonly FlexColumnView _content;
    private readonly AddRepoButton _addButton = new();
    private readonly Dictionary<Guid, GroupSection> _sections = new();
    private IMessageBus? _bus;
    private IRepoRegistry? _registry;

    public RepoBar()
    {
        PreferredWidth = BarWidth;
        _content = new FlexColumnView
        {
            Gap = 2,
            CrossAxisAlignment = CrossAxisAlignment.Stretch,
        };
        var root = new FlexColumnView
        {
            Gap = 6,
            CrossAxisAlignment = CrossAxisAlignment.Stretch,
            Children = { _content, _addButton }
        };
        root.UpdateStyle(_content, new FlexStyle { Grow = 1 });
        AddChildToSelf(new RectView
        {
            BackgroundColor = DialogPalette.Background,
            BorderColor = new BorderColorStyle { Right = DialogPalette.Border },
            BorderSize = new BorderSizeStyle { Right = 1 },
            Padding = new PaddingStyle
            {
                Left = HorizontalPadding,
                Right = HorizontalPadding,
                Top = HorizontalPadding,
                Bottom = HorizontalPadding,
            },
            Children = { root }
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
        if (_registry is null) return;
        var activeId = _registry.Active?.Id;
        var reposById = _registry.Repos.ToDictionary(r => r.Id);

        var liveGroupIds = _registry.Groups.Select(g => g.Id).ToHashSet();
        foreach (var id in _sections.Keys.ToList())
        {
            if (liveGroupIds.Contains(id)) continue;
            _content.Children.Remove(_sections[id]);
            _sections.Remove(id);
        }

        foreach (var group in _registry.Groups)
        {
            if (!_sections.TryGetValue(group.Id, out var section))
            {
                section = new GroupSection(group);
                _sections[group.Id] = section;
                _content.Children.Add(section);
            }
            section.Update(group, activeId, reposById);
        }
    }
}

public sealed class GroupSection : View
{
    private readonly GroupHeaderRow _header;
    private readonly FlexColumnView _rows;

    public GroupSection(Group group)
    {
        _header = new GroupHeaderRow(group);
        _rows = new FlexColumnView
        {
            Gap = 2,
            CrossAxisAlignment = CrossAxisAlignment.Stretch,
        };
        AddChildToSelf(new FlexColumnView
        {
            Gap = 2,
            CrossAxisAlignment = CrossAxisAlignment.Stretch,
            Children = { _header, _rows }
        });
    }

    public void Update(Group group, Guid? activeId, Dictionary<Guid, Repo> reposById)
    {
        _header.Update(group);
        _rows.Children.Clear();
        foreach (var repoId in group.RepoIds)
        {
            if (!reposById.TryGetValue(repoId, out var repo)) continue;
            var isActive = repo.Id == activeId;
            if (group.IsCollapsed && !isActive) continue;
            _rows.Children.Add(new RepoRow(repo, isActive));
        }
    }
}

public sealed class AddRepoButton : View
{
    public AddRepoButton()
    {
        PreferredHeight = 30;

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
                    Text = "+  Add Repository",
                    TextColor = DialogPalette.RowText,
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

public sealed class GroupHeaderRow : View
{
    private readonly TextView _chevron;
    private readonly TextView _name;

    public GroupHeaderRow(Group group)
    {
        PreferredHeight = 26;

        _chevron = new TextView
        {
            Text = ChevronFor(group.IsCollapsed),
            TextColor = DialogPalette.SectionHeaderText,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            PreferredWidth = 16,
        };
        _name = new TextView
        {
            Text = group.Name,
            TextColor = DialogPalette.SectionHeaderText,
            HorizontalTextAlignment = TextAlignment.Start,
            VerticalTextAlignment = TextAlignment.Center,
        };
        var row = new FlexRowView
        {
            CrossAxisAlignment = CrossAxisAlignment.Center,
            Gap = 4,
            Children = { _chevron, _name }
        };
        var background = new RectView
        {
            BackgroundColor = DialogPalette.RowTransparent,
            BorderRadius = BorderRadiusStyle.All(4),
            Padding = new PaddingStyle { Left = 2, Right = 8 },
            Children = { row }
        };
        AddChildToSelf(background);

        Behaviors.Add(new HoverableButtonController(
            () => Context?.Get<IRepoRegistry>()?.ToggleGroupCollapsed(group.Id),
            isHovered =>
            {
                background.BackgroundColor = isHovered ? DialogPalette.RowHover : DialogPalette.RowTransparent;
            }));
    }

    public void Update(Group group)
    {
        _chevron.Text = ChevronFor(group.IsCollapsed);
        _name.Text = group.Name;
    }

    private static string ChevronFor(bool isCollapsed) => isCollapsed ? "▶" : "▼";
}

public sealed class RepoRow : View
{
    private readonly Repo _repo;
    private readonly TextView _label;

    public RepoRow(Repo repo, bool isActive)
    {
        _repo = repo;
        PreferredHeight = 28;

        var baseBg = isActive ? DialogPalette.RowActive : DialogPalette.RowTransparent;
        var hoverBg = isActive ? DialogPalette.RowActive : DialogPalette.RowHover;
        var textColor = repo.IsMissing
            ? DialogPalette.RowTextMissing
            : (isActive ? DialogPalette.RowTextActive : DialogPalette.RowText);

        _label = new TextView
        {
            Text = repo.DisplayName,
            TextColor = textColor,
            HorizontalTextAlignment = TextAlignment.Start,
            VerticalTextAlignment = TextAlignment.Center,
        };
        var background = new RectView
        {
            BackgroundColor = baseBg,
            BorderRadius = BorderRadiusStyle.All(4),
            Padding = new PaddingStyle { Left = 24, Right = 12 },
            Children = { _label }
        };
        AddChildToSelf(background);

        Behaviors.Add(new HoverableButtonController(
            () => Context?.Get<IRepoRegistry>()?.SetActive(repo.Id),
            isHovered =>
            {
                background.BackgroundColor = isHovered ? hoverBg : baseBg;
            }));
    }

    protected override void OnAttachedToContext(Context context)
    {
        base.OnAttachedToContext(context);
        _label.Text = TruncateToFit(_repo.DisplayName, context);
    }

    private static string TruncateToFit(string text, Context context)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var available = RepoBar.RowTextAvailableWidth;
        if (available <= 0)
            return text;

        if (Measure(text, context) <= available)
            return text;

        const string ellipsis = "…";
        var ellipsisWidth = Measure(ellipsis, context);
        if (ellipsisWidth > available)
            return ellipsis;

        var lo = 0;
        var hi = text.Length;
        while (lo < hi)
        {
            var mid = (lo + hi + 1) / 2;
            if (Measure(text[..mid], context) + ellipsisWidth <= available)
                lo = mid;
            else
                hi = mid - 1;
        }
        return text[..lo] + ellipsis;
    }

    private static float Measure(string s, Context context)
    {
        var probe = new TextView { Text = s, Context = context };
        return probe.MeasureWidth();
    }
}

public record struct AddRepoMessage;