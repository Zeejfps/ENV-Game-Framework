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

        uint RowTextColor() => RowChrome.RowTextColor(registry, repo);

        // Chevron slot is always present so primaries with and without worktrees share
        // alignment. The slot becomes interactive (and visible) only when children exist.
        var chevronSlot = new WorktreeChevron(repo, registry);

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
                        chevronSlot,
                        icon,
                        new FlexItem { Grow = 1, Child = _label },
                    }
                }
            }
        };
        RowChrome.BindRowBackground(background, isHovered, registry, repo.Id);
        AddChildToSelf(background);

        this.UseController(ctx => new RepoRowController(
            this, ctx,
            repo,
            registry,
            h => isHovered.Value = h,
            _ => BuildMenuItems(repo, registry, ctx)));
    }

    private static IReadOnlyList<RepoBarContextMenu.Item> BuildMenuItems(Repo repo, IRepoRegistry registry, Context context)
    {
        var sourceGroup = registry.FindGroupContaining(repo.Id);
        var items = new List<RepoBarContextMenu.Item>();

        var bus = context.Get<IMessageBus>();
        if (bus is not null)
        {
            items.Add(new RepoBarContextMenu.Item(
                "New worktree…",
                () => bus.Broadcast(new ShowDialogMessage(onClose => new CreateWorktreeDialog(repo, onClose))),
                LucideIcons.Branch));

            var git = context.Get<IGitService>();
            if (git is not null)
            {
                items.Add(new RepoBarContextMenu.Item(
                    "Prune worktrees",
                    () =>
                    {
                        Task.Run(() => git.PruneWorktrees(repo));
                        bus.Broadcast(new WorktreesChangedMessage(repo.Id));
                    },
                    LucideIcons.Trash));
            }

            items.Add(new RepoBarContextMenu.Item(
                "Add submodule…",
                () => bus.Broadcast(new ShowDialogMessage(onClose => new AddSubmoduleDialog(repo, onClose))),
                LucideIcons.Package));

            items.Add(new RepoBarContextMenu.Item(
                "Update all submodules…",
                () => bus.Broadcast(new ShowDialogMessage(onClose => new UpdateSubmodulesDialog(repo, null, onClose))),
                LucideIcons.Pull));
        }

        foreach (var group in registry.Groups)
        {
            if (sourceGroup != null && group.Id == sourceGroup.Id) continue;
            var captured = group;
            items.Add(new RepoBarContextMenu.Item(
                $"Move to: {captured.Name}",
                () => registry.MoveRepo(repo.Id, captured.Id, captured.RepoIds.Count),
                LucideIcons.FolderInput));
        }

        items.Add(new RepoBarContextMenu.Item(
            "Remove repo",
            () => registry.RemoveRepo(repo.Id),
            LucideIcons.Trash));

        items.Add(new RepoBarContextMenu.Item(
            "New group",
            () =>
            {
                var id = registry.CreateGroup("New Group");
                registry.BeginRenameGroup(id);
            },
            LucideIcons.FolderPlus));

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
