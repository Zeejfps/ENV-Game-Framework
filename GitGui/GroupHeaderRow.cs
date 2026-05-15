using ZGF.Gui;
using ZGF.Gui.Layouts;

namespace GitGui;

public sealed class GroupHeaderRow : MultiChildView
{
    private readonly TextView _chevron;
    private readonly TextView _name;

    public GroupHeaderRow(Group group)
    {
        PreferredHeight = 26;

        _chevron = new TextView
        {
            Text = ChevronFor(group.IsCollapsed),
            TextColor = DialogPalette.SectionHeaderText,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            PreferredWidth = 16,
        };
        _name = new TextView
        {
            Text = group.Name,
            TextColor = DialogPalette.SectionHeaderText,
            HorizontalTextAlignment = TextAlignment.Start,
            VerticalTextAlignment = TextAlignment.Center,
        };
        var row = new FlexRowView
        {
            CrossAxisAlignment = CrossAxisAlignment.Center,
            Gap = 4,
            Children = { _chevron, _name }
        };
        var background = new RectView
        {
            BackgroundColor = DialogPalette.RowTransparent,
            BorderRadius = BorderRadiusStyle.All(4),
            Padding = new PaddingStyle { Left = 2, Right = 8 },
            Children = { row }
        };
        AddChildToSelf(background);

        Behaviors.Add(new HoverableButtonController(
            () => Context?.Get<IRepoRegistry>()?.ToggleGroupCollapsed(group.Id),
            isHovered =>
            {
                background.BackgroundColor = isHovered ? DialogPalette.RowHover : DialogPalette.RowTransparent;
            }));
    }

    public void Update(Group group)
    {
        _chevron.Text = ChevronFor(group.IsCollapsed);
        _name.Text = group.Name;
    }

    private static string ChevronFor(bool isCollapsed) => isCollapsed ? "▶" : "▼";
}