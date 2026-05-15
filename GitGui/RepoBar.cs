using ZGF.Gui;
using ZGF.Gui.Layouts;

namespace GitGui;

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