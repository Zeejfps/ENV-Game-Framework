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
            FontSize = 18f,
            TextVerticalAlignment = TextAlignment.Center,
        };
        input.BindToTheme(t =>
        {
            input.BackgroundColor = t.Dialog.ButtonNormal;
            input.TextColor = t.Dialog.TitleText;
            input.CaretColor = t.Dialog.TitleText;
            input.SelectionRectColor = t.Dialog.RowActive;
        });
        input.Enter(group.Name);
        input.SelectAll();

        var frame = new RectView
        {
            BorderSize = BorderSizeStyle.All(1),
            BorderRadius = BorderRadiusStyle.All(3),
            Padding = new PaddingStyle { Left = 4, Right = 4 },
            Children = { input }
        };
        frame.BindBackgroundColorFromTheme(t => t.Dialog.ButtonNormal);
        frame.BindBorderColorFromTheme(t => BorderColorStyle.All(t.Dialog.ButtonBorderHover));
        AddChildToSelf(frame);

        this.UseController(ctx =>
        {
            var inputSystem = ctx.Require<InputSystem>();
            return new GroupRenameKbmController(input, inputSystem, group.Id, registry);
        });
    }
}