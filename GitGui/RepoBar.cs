using ZGF.Gui;
using ZGF.Gui.Bindings;
using ZGF.Gui.Layouts;

namespace GitGui;

internal sealed class RepoBar : MultiChildView, IBind<RepoBarViewModel>
{
    private const int BarWidth = 220;
    private const int HorizontalPadding = 8;
    internal const int RowPaddingLeft = 12;
    internal const int RowChevronWidth = 12;
    internal const int RowIconWidth = 16;
    internal const int RowIconGap = 6;
    internal const int WorktreeRowExtraIndent = 16;
    private const int RowTextIndent = RowPaddingLeft + RowChevronWidth + RowIconGap + RowIconWidth + RowIconGap;
    private const int RowTextRightPadding = 12;

    public static int RowTextAvailableWidth =>
        BarWidth - 2 * HorizontalPadding - RowTextIndent - RowTextRightPadding;

    public static int WorktreeRowTextAvailableWidth =>
        RowTextAvailableWidth - WorktreeRowExtraIndent;

    private readonly FlexColumnView _sections;
    private RepoBarViewModel? _vm;

    public RepoBar()
    {
        _sections = new FlexColumnView
        {
            Gap = 2,
            CrossAxisAlignment = CrossAxisAlignment.Stretch,
        };

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
                        new FlexItem { Grow = 1, Child = _sections },
                        new AddRepoButton(),
                    }
                }
            }
        });

        this.UseController(ctx => new RepoBarContextMenuController(ctx, _ => BuildBackgroundMenuItems()));
        this.UseViewModel(this);
    }

    public void Bind(RepoBarViewModel vm)
    {
        _vm = vm;
        _sections.BindChildren(
            vm.GroupSections,
            _ => new GroupSection(),
            onCreated: (section, sectionVm) => section.Bind(sectionVm));
    }

    private IReadOnlyList<RepoBarContextMenu.Item> BuildBackgroundMenuItems()
    {
        return
        [
            new RepoBarContextMenu.Item("New group", () => _vm?.NewGroup.Execute(), LucideIcons.FolderPlus),
        ];
    }
}
