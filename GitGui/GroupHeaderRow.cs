using ZGF.Gui;
using ZGF.Gui.Bindings;
using ZGF.Gui.Layouts;
using ZGF.Observable;

namespace GitGui;

public sealed class GroupHeaderRow : MultiChildView
{
    public GroupHeaderRow(Group group, IRepoRegistry registry)
    {
        PreferredHeight = 26;

        var isHovered = new State<bool>(false);

        var chevron = new TextView
        {
            Text = ChevronFor(group.IsCollapsed),
            TextColor = DialogPalette.SectionHeaderText,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            PreferredWidth = 16,
        };
        var name = new TextView
        {
            Text = group.Name,
            TextColor = DialogPalette.SectionHeaderText,
            HorizontalTextAlignment = TextAlignment.Start,
            VerticalTextAlignment = TextAlignment.Center,
        };
        var background = new RectView
        {
            BorderRadius = BorderRadiusStyle.All(4),
            Padding = new PaddingStyle { Left = 2, Right = 8 },
            Children =
            {
                new FlexRowView
                {
                    CrossAxisAlignment = CrossAxisAlignment.Center,
                    Gap = 4,
                    Children = { chevron, name }
                }
            }
        };
        background.BindBackgroundColor(isHovered,
            h => h ? DialogPalette.RowHover : DialogPalette.RowTransparent);
        AddChildToSelf(background);

        Behaviors.Add(new HoverableButtonController(
            () => registry.ToggleGroupCollapsed(group.Id),
            h => isHovered.Value = h));
    }

    private static string ChevronFor(bool isCollapsed) => isCollapsed ? "▶" : "▼";
}
