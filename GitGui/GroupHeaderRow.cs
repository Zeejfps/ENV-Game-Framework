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
            FontSize = 8f,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            PreferredWidth = 16,
        };

        var nameSlot = new MultiChildView();
        nameSlot.BindChildren<bool, View>(
            () => new[] { registry.RenamingGroupId.Value == group.Id },
            isRenaming => isRenaming
                ? new GroupRenameField(group, registry)
                : new TextView
                {
                    Text = group.Name,
                    TextColor = DialogPalette.SectionHeaderText,
                    FontSize = 18f,
                    HorizontalTextAlignment = TextAlignment.Start,
                    VerticalTextAlignment = TextAlignment.Center,
                });

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
                        new FlexItem { Grow = 1, Child = nameSlot },
                    }
                }
            }
        };
        background.BindBackgroundColor(isHovered,
            h => h ? DialogPalette.RowHover : DialogPalette.RowTransparent);
        AddChildToSelf(background);

        Behaviors.Add(new GroupHeaderController(
            group,
            registry,
            h => isHovered.Value = h,
            _ => BuildMenuItems(group, registry)));
    }

    private static IReadOnlyList<RepoBarContextMenu.Item> BuildMenuItems(Group group, IRepoRegistry registry)
    {
        var items = new List<RepoBarContextMenu.Item>
        {
            new("Rename group", () => registry.BeginRenameGroup(group.Id)),
        };

        if (registry.Groups.Count > 1)
        {
            items.Add(new RepoBarContextMenu.Item("Delete group", () => registry.DeleteGroup(group.Id)));
        }

        items.Add(new RepoBarContextMenu.Item("New group", () =>
        {
            var id = registry.CreateGroup("New Group");
            registry.BeginRenameGroup(id);
        }));

        return items;
    }

    private static string ChevronFor(bool isCollapsed) => isCollapsed ? "▶" : "▼";
}
