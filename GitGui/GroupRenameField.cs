using ZGF.Gui;
using ZGF.Gui.Tests;

namespace GitGui;

public sealed class GroupRenameField : MultiChildView
{
    public GroupRenameField(Group group, IRepoRegistry registry)
    {
        PreferredHeight = 22;

        var input = new TextInputView
        {
            BackgroundColor = DialogPalette.ButtonNormal,
            TextColor = DialogPalette.TitleText,
            FontSize = 18f,
            CaretColor = DialogPalette.TitleText,
            SelectionRectColor = DialogPalette.RowActive,
            TextVerticalAlignment = TextAlignment.Center,
        };
        input.Enter(group.Name);
        input.SelectAll();

        AddChildToSelf(new RectView
        {
            BackgroundColor = DialogPalette.ButtonNormal,
            BorderColor = BorderColorStyle.All(DialogPalette.ButtonBorderHover),
            BorderSize = BorderSizeStyle.All(1),
            BorderRadius = BorderRadiusStyle.All(3),
            Padding = new PaddingStyle { Left = 4, Right = 4 },
            Children = { input }
        });

        this.UseController(ctx =>
        {
            var inputSystem = ctx.Require<InputSystem>();
            return new GroupRenameKbmController(input, inputSystem, group.Id, registry);
        });
    }
}