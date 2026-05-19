using ZGF.Gui;
using ZGF.Gui.Bindings;
using ZGF.Gui.Layouts;
using ZGF.Observable;

namespace GitGui;

public sealed class GroupHeaderRow : MultiChildView, IGroupHeaderRowView
{
    private readonly MultiChildView _nameSlot;

    public event Action? ToggleCollapsedRequested;
    public Func<IReadOnlyList<RepoBarContextMenu.Item>>? MenuItemsProvider { private get; set; }
    public Func<bool>? IsRenamingProvider { private get; set; }

    public GroupHeaderRow(Group group)
    {
        PreferredHeight = 26;

        var isHovered = new State<bool>(false);

        var chevron = new TextView
        {
            Text = ChevronFor(group.IsCollapsed),
            TextColor = DialogPalette.SectionHeaderText,
            FontSize = 8f,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            PreferredWidth = 16,
        };

        _nameSlot = new MultiChildView();

        var background = new RectView
        {
            BorderRadius = BorderRadiusStyle.All(4),
            Padding = new PaddingStyle { Left = 2, Right = 8 },
            Children =
            {
                new FlexRowView
                {
                    CrossAxisAlignment = CrossAxisAlignment.Center,
                    Gap = 8,
                    Children =
                    {
                        chevron,
                        new FlexItem { Grow = 1, Child = _nameSlot },
                    }
                }
            }
        };
        background.BindBackgroundColor(isHovered,
            h => h ? DialogPalette.RowHover : DialogPalette.RowTransparent);
        AddChildToSelf(background);

        this.UseController(ctx => new GroupHeaderController(
            this, ctx,
            group,
            h => isHovered.Value = h,
            _ => MenuItemsProvider?.Invoke() ?? Array.Empty<RepoBarContextMenu.Item>(),
            () => IsRenamingProvider?.Invoke() ?? false,
            () => ToggleCollapsedRequested?.Invoke()));

        this.UsePresenter(ctx => new GroupHeaderRowPresenter(this, group, ctx.Require<IRepoRegistry>()));
    }

    public void BindName(Func<IEnumerable<bool>> isRenamingCompute, Func<bool, View> contentFactory)
    {
        _nameSlot.BindChildren(isRenamingCompute, contentFactory);
    }

    private static string ChevronFor(bool isCollapsed) => isCollapsed ? "▶" : "▼";
}
