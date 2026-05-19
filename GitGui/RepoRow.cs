using ZGF.Gui;
using ZGF.Gui.Bindings;
using ZGF.Gui.Layouts;
using ZGF.Observable;

namespace GitGui;

public sealed class RepoRow : MultiChildView
{
    private readonly Repo _repo;
    private readonly TextView _label;

    public RepoRow(Repo repo, IRepoRegistry registry)
    {
        _repo = repo;
        PreferredHeight = 28;

        var isHovered = new State<bool>(false);

        uint RowTextColor()
        {
            if (repo.IsMissing) return DialogPalette.RowTextMissing;
            return registry.Active.Value?.Id == repo.Id
                ? DialogPalette.RowTextActive
                : DialogPalette.RowText;
        }

        var icon = new TextView
        {
            Text = LucideIcons.FolderGit2,
            FontFamily = LucideIcons.FontFamily,
            FontSize = 14,
            PreferredWidth = RepoBar.RowIconWidth,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
        };
        icon.BindTextColor(RowTextColor);

        _label = new TextView
        {
            Text = repo.DisplayName,
            HorizontalTextAlignment = TextAlignment.Start,
            VerticalTextAlignment = TextAlignment.Center,
        };
        _label.BindTextColor(RowTextColor);

        var background = new RectView
        {
            BorderRadius = BorderRadiusStyle.All(4),
            Padding = new PaddingStyle { Left = RepoBar.RowPaddingLeft, Right = 12 },
            Children =
            {
                new FlexRowView
                {
                    Gap = RepoBar.RowIconGap,
                    CrossAxisAlignment = CrossAxisAlignment.Center,
                    Children =
                    {
                        icon,
                        new FlexItem { Grow = 1, Child = _label },
                    }
                }
            }
        };
        background.BindBackgroundColor(() =>
        {
            var active = registry.Active.Value?.Id == repo.Id;
            return (isHovered.Value, active) switch
            {
                (_, true) => DialogPalette.RowActive,
                (true, false) => DialogPalette.RowHover,
                _ => DialogPalette.RowTransparent,
            };
        });
        AddChildToSelf(background);

        this.UseController(ctx => new RepoRowController(
            this, ctx,
            repo,
            registry,
            h => isHovered.Value = h,
            _ => BuildMenuItems(repo, registry)));
    }

    private static IReadOnlyList<RepoBarContextMenu.Item> BuildMenuItems(Repo repo, IRepoRegistry registry)
    {
        var sourceGroup = registry.FindGroupContaining(repo.Id);
        var items = new List<RepoBarContextMenu.Item>();

        foreach (var group in registry.Groups)
        {
            if (sourceGroup != null && group.Id == sourceGroup.Id) continue;
            var captured = group;
            items.Add(new RepoBarContextMenu.Item(
                $"Move to: {captured.Name}",
                () => registry.MoveRepo(repo.Id, captured.Id, captured.RepoIds.Count)));
        }

        items.Add(new RepoBarContextMenu.Item(
            "Remove repo",
            () => registry.RemoveRepo(repo.Id)));

        items.Add(new RepoBarContextMenu.Item(
            "New group",
            () =>
            {
                var id = registry.CreateGroup("New Group");
                registry.BeginRenameGroup(id);
            }));

        return items;
    }

    protected override void OnAttachedToContext(Context context)
    {
        base.OnAttachedToContext(context);
        _label.Text = TextMeasure.TruncateToFit(
            _repo.DisplayName, _labelStyle, RepoBar.RowTextAvailableWidth, context.Canvas);
    }

    // Same defaults TextView would apply to _label; reused for measurement-only purposes.
    private static readonly TextStyle _labelStyle = new();
}
