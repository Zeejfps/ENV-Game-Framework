using ZGF.Gui;
using ZGF.Gui.Bindings;
using ZGF.Gui.Layouts;
using ZGF.Observable;

namespace GitGui;

public sealed class RepoBar : MultiChildView, IRepoBarView
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

    private readonly FlexColumnView _sections;

    public event Action? NewGroupRequested;

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
        this.UsePresenter(ctx => new RepoBarPresenter(this, ctx.Require<IRepoRegistry>()));
    }

    public void BindGroups(ObservableList<Group> groups, Func<Group, View> sectionFactory)
    {
        _sections.BindChildren(groups, sectionFactory);
    }

    private IReadOnlyList<RepoBarContextMenu.Item> BuildBackgroundMenuItems()
    {
        return
        [
            new RepoBarContextMenu.Item("New group", () => NewGroupRequested?.Invoke()),
        ];
    }
}
