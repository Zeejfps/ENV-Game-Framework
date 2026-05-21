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
    private readonly Repo _submodule;
    private readonly TextView _label;

    public SubmoduleRow(Repo submodule, IRepoRegistry registry)
    {
        _submodule = submodule;
        PreferredHeight = 26;

        var isHovered = new State<bool>(false);

        uint RowTextColor()
        {
            if (submodule.IsMissing) return DialogPalette.RowTextMissing;
            return registry.Active.Value?.Id == submodule.Id
                ? DialogPalette.RowTextActive
                : DialogPalette.RowText;
        }

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

        _label = new TextView
        {
            Text = submodule.DisplayName,
            HorizontalTextAlignment = TextAlignment.Start,
            VerticalTextAlignment = TextAlignment.Center,
        };
        _label.BindTextColor(RowTextColor);

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
        background.BindBackgroundColor(() =>
        {
            var active = registry.Active.Value?.Id == submodule.Id;
            return (isHovered.Value, active) switch
            {
                (_, true) => DialogPalette.RowActive,
                (true, false) => DialogPalette.RowHover,
                _ => DialogPalette.RowTransparent,
            };
        });
        AddChildToSelf(background);

        this.UseController(ctx => new SubmoduleRowController(
            ctx,
            submodule,
            registry,
            h => isHovered.Value = h,
            _ => BuildMenuItems(submodule, registry, ctx)));
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
                    () => bus.Broadcast(new ShowUpdateSubmodulesDialogMessage(primary, submodule)),
                    LucideIcons.Pull));
                items.Add(new RepoBarContextMenu.Item(
                    "Deinit submodule…",
                    () => bus.Broadcast(new ShowDeinitSubmoduleDialogMessage(primary, submodule)),
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
            _submodule.DisplayName, _labelStyle, RepoBar.WorktreeRowTextAvailableWidth, context.Canvas);
    }

    private static readonly TextStyle _labelStyle = new();
}
