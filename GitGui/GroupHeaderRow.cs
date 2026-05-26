using ZGF.Gui;
using ZGF.Gui.Bindings;
using ZGF.Gui.Layouts;
using ZGF.Observable;

namespace GitGui;

internal sealed class GroupHeaderRow : MultiChildView, IBind<GroupHeaderRowViewModel>
{
    private readonly MultiChildView _nameSlot;
    private readonly TextView _chevron;
    private readonly State<bool> _isHovered = new(false);

    public GroupHeaderRow()
    {
        PreferredHeight = 26;

        _chevron = new TextView
        {
            FontSize = 8f,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            PreferredWidth = 16,
        };
        _chevron.BindTextColorFromTheme(t => t.Dialog.SectionHeaderText);

        _nameSlot = new MultiChildView();

        var hoverBg = new State<uint>(ThemePresets.Dark.Dialog.RowHover);
        var transparentBg = new State<uint>(ThemePresets.Dark.Dialog.RowTransparent);

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
                        _chevron,
                        new FlexItem { Grow = 1, Child = _nameSlot },
                    }
                }
            }
        };
        background.BindToTheme(t =>
        {
            hoverBg.Value = t.Dialog.RowHover;
            transparentBg.Value = t.Dialog.RowTransparent;
        });
        background.BindBackgroundColor(() => _isHovered.Value ? hoverBg.Value : transparentBg.Value);
        AddChildToSelf(background);
    }

    public void Bind(GroupHeaderRowViewModel vm)
    {
        _chevron.Text = ChevronFor(vm.Group.IsCollapsed);

        _nameSlot.BindChildren(
            () => new[] { vm.IsRenaming.Value },
            vm.CreateNameContent);

        this.UseController(ctx => new GroupHeaderController(
            this, ctx,
            vm.Group,
            h => _isHovered.Value = h,
            _ => vm.BuildMenuItems(),
            () => vm.IsRenaming.Value,
            vm.ToggleCollapsed.Execute));
    }

    private static string ChevronFor(bool isCollapsed) => isCollapsed ? "▶" : "▼";
}
