using ZGF.Gui;
using ZGF.Gui.Bindings;
using ZGF.Gui.Layouts;
using ZGF.Observable;

namespace GitGui;

// Renders a single submodule row nested under its parent in the RepoBar. Same shape
// as WorktreeRow (deep indent + small icon) but uses the Package icon + purple tint
// to signal "this is an embedded external repository pinned to a specific commit,"
// not a sibling checkout of the parent.
public sealed class SubmoduleRow : MultiChildView
{
    public SubmoduleRow(Repo submodule, IRepoRegistry registry)
    {
        PreferredHeight = 26;

        var isHovered = new State<bool>(false);

        uint RowTextColor() => RowChrome.RowTextColor(registry, submodule);

        var icon = new TextView
        {
            Text = LucideIcons.Package,
            FontFamily = LucideIcons.FontFamily,
            FontSize = 13,
            PreferredWidth = RepoBar.RowIconWidth,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
        };
        // Package icon + purple tint — submodules are mentally "external packages
        // embedded at a pinned commit," visually distinct from the FolderGit2 used for
        // primary repos. Tint matches the StatusSubmodule badge used by pointer-change
        // rows in CommitDetails so the visual language stays consistent across the app.
        icon.BindTextColor(() => submodule.IsMissing
            ? DialogPalette.RowTextMissing
            : DialogPalette.IconAccentSubmodule);

        var label = new TextView
        {
            Text = submodule.DisplayName,
            HorizontalTextAlignment = TextAlignment.Start,
            VerticalTextAlignment = TextAlignment.Center,
            TextOverflow = TextOverflow.Ellipsis,
        };
        label.BindTextColor(RowTextColor);

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
                        new FlexItem { Grow = 1, Child = label },
                    }
                }
            }
        };
        RowChrome.BindRowBackground(background, isHovered, registry, submodule.Id);
        AddChildToSelf(background);

        this.UseController(ctx => new NavigableRowController(
            ctx,
            submodule.Id,
            registry,
            h => isHovered.Value = h,
            _ => BuildMenuItems(submodule, registry, ctx),
            // A submodule that's been added but not initialized has no .git directory of its
            // own and would render an empty BranchesView/HistoryView. Better to do nothing —
            // the user can right-click → Update… to initialize it.
            canActivate: () => !submodule.IsMissing));
    }

    private static IReadOnlyList<RepoBarContextMenu.Item> BuildMenuItems(
        Repo submodule, IRepoRegistry registry, Context context)
    {
        var items = new List<RepoBarContextMenu.Item>();

        if (!submodule.IsMissing)
        {
            items.Add(new RepoBarContextMenu.Item(
                "Switch to submodule",
                () => registry.SetActive(submodule.Id),
                LucideIcons.Package));
        }

        var shell = context.Get<IPlatformShell>();
        if (shell is not null)
        {
            items.Add(new RepoBarContextMenu.Item(
                "Open folder",
                () => shell.OpenFolder(submodule.Path),
                LucideIcons.FolderOpen));
        }

        var bus = context.Get<IMessageBus>();
        if (bus is not null && submodule.ParentRepoId is { } parentId)
        {
            var primary = FindPrimary(registry, parentId);
            if (primary is not null)
            {
                items.Add(new RepoBarContextMenu.Item(
                    "Update submodule…",
                    () => bus.Broadcast(new ShowDialogMessage(onClose => new UpdateSubmodulesDialog(primary, submodule, onClose))),
                    LucideIcons.Pull));
                items.Add(new RepoBarContextMenu.Item(
                    "Deinit submodule…",
                    () => bus.Broadcast(new ShowDialogMessage(onClose => new DeinitSubmoduleDialog(primary, submodule, onClose))),
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

}
