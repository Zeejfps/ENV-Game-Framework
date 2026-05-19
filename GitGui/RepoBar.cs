using ZGF.Gui;
using ZGF.Gui.Bindings;
using ZGF.Gui.Layouts;

namespace GitGui;

public sealed class RepoBar : MultiChildView
{
    private const int BarWidth = 220;
    private const int HorizontalPadding = 8;
    internal const int RowPaddingLeft = 24;
    internal const int RowIconWidth = 16;
    internal const int RowIconGap = 6;
    private const int RowTextIndent = RowPaddingLeft + RowIconWidth + RowIconGap;
    private const int RowTextRightPadding = 12;

    public static int RowTextAvailableWidth =>
        BarWidth - 2 * HorizontalPadding - RowTextIndent - RowTextRightPadding;

    public RepoBar(IRepoRegistry registry)
    {
        var sections = new FlexColumnView
        {
            Gap = 2,
            CrossAxisAlignment = CrossAxisAlignment.Stretch,
        };
        sections.BindChildren(registry.Groups, group => new GroupSection(group, registry));

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
            Children =
            {
                new FlexColumnView
                {
                    Gap = 6,
                    CrossAxisAlignment = CrossAxisAlignment.Stretch,
                    Children =
                    {
                        new FlexItem { Grow = 1, Child = sections },
                        new AddRepoButton(),
                    }
                }
            }
        });

        this.UseController(ctx => new RepoBarContextMenuController(ctx, _ => BuildBackgroundMenuItems(registry)));
    }

    private static IReadOnlyList<RepoBarContextMenu.Item> BuildBackgroundMenuItems(IRepoRegistry registry)
    {
        return
        [
            new RepoBarContextMenu.Item("New group", () =>
            {
                var id = registry.CreateGroup("New Group");
                registry.BeginRenameGroup(id);
            })
        ];
    }
}
