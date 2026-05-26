using ZGF.Gui;
using ZGF.Gui.Bindings;
using ZGF.Gui.Layouts;
using ZGF.Observable;

namespace GitGui;

// Renders a single worktree row, nested under its primary in the RepoBar. Visually
// distinguished from primary rows by deeper indent and the Branch icon.
public sealed class WorktreeRow : MultiChildView
{
    private readonly Repo _worktree;
    private readonly TextView _label;

    public WorktreeRow(Repo worktree, IRepoRegistry registry)
    {
        _worktree = worktree;
        PreferredHeight = 26;

        var isHovered = new State<bool>(false);
        var tokens = new State<ThemeTokens>(ThemePresets.Dark);

        var icon = new TextView
        {
            Text = LucideIcons.Branch,
            FontFamily = LucideIcons.FontFamily,
            FontSize = 13,
            PreferredWidth = RepoBar.RowIconWidth,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
        };
        // Tinted by kind so the sidebar tells worktree apart from primary / submodule
        // without leaning on a header row. Missing rows mute the accent to match the label.
        icon.BindToTheme(t => tokens.Value = t);
        icon.BindTextColor(() => worktree.IsMissing
            ? tokens.Value.Dialog.RowTextMissing
            : tokens.Value.Dialog.IconAccentWorktree);

        _label = new TextView
        {
            Text = worktree.DisplayName,
            HorizontalTextAlignment = TextAlignment.Start,
            VerticalTextAlignment = TextAlignment.Center,
        };
        RowChrome.BindRowTextColor(_label, registry, worktree);

        // Indent past the primary's chevron+icon column. The math mirrors the constants
        // RepoRow uses internally so children appear nested visually.
        var leftPad = RepoBar.RowPaddingLeft
                      + RepoBar.RowChevronWidth
                      + RepoBar.RowIconGap
                      + RepoBar.WorktreeRowExtraIndent;

        var background = new RectView
        {
            BorderRadius = BorderRadiusStyle.All(4),
            Padding = new PaddingStyle { Left = leftPad, Right = 12 },
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
        RowChrome.BindRowBackground(background, isHovered, registry, worktree.Id);
        AddChildToSelf(background);

        this.UseController(ctx => new NavigableRowController(
            ctx,
            worktree.Id,
            registry,
            h => isHovered.Value = h,
            _ => BuildMenuItems(worktree, registry, ctx)));
    }

    private static IReadOnlyList<RepoBarContextMenu.Item> BuildMenuItems(
        Repo worktree, IRepoRegistry registry, Context context)
    {
        var items = new List<RepoBarContextMenu.Item>();

        items.Add(new RepoBarContextMenu.Item(
            "Switch to worktree",
            () => registry.SetActive(worktree.Id),
            LucideIcons.Branch));

        var shell = context.Get<IPlatformShell>();
        if (shell is not null)
        {
            items.Add(new RepoBarContextMenu.Item(
                "Open folder",
                () => shell.OpenFolder(worktree.Path),
                LucideIcons.FolderOpen));
        }

        var bus = context.Get<IMessageBus>();
        if (bus is not null && worktree.ParentRepoId is { } parentId)
        {
            var primary = FindPrimary(registry, parentId);
            if (primary is not null)
            {
                items.Add(new RepoBarContextMenu.Item(
                    "Remove worktree…",
                    () => bus.Broadcast(new ShowDialogMessage(onClose => new RemoveWorktreeDialog(primary, worktree, onClose))),
                    LucideIcons.Trash));
            }
        }

        return items;
    }

    private static Repo? FindPrimary(IRepoRegistry registry, Guid id)
    {
        foreach (var r in registry.Repos)
        {
            if (r.Id == id) return r;
        }
        return null;
    }

    protected override void OnAttachedToContext(Context context)
    {
        base.OnAttachedToContext(context);
        _label.Text = TextMeasure.TruncateToFit(
            _worktree.DisplayName, _labelStyle, RepoBar.WorktreeRowTextAvailableWidth, context.Canvas);
    }

    private static readonly TextStyle _labelStyle = new();
}
