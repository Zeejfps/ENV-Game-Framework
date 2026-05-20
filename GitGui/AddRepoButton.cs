using ZGF.Gui;

namespace GitGui;

public sealed class AddRepoButton : HoverableButton
{
    public AddRepoButton()
    {
        PreferredHeight = 30;

        var background = new RectView
        {
            BorderSize = BorderSizeStyle.All(1),
            BorderRadius = BorderRadiusStyle.All(6),
            Children =
            {
                new TextView
                {
                    Text = "+  Add Repository",
                    TextColor = DialogPalette.RowText,
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center,
                }
            }
        };
        DialogPalette.BindBorderedButtonChrome(background, IsHovered);
        SetBackground(background);
    }

    protected override void OnClicked()
    {
        var path = Context?.Get<IPlatformShell>()?.PickFolder("Open Repository");
        if (string.IsNullOrEmpty(path)) return;
        Context?.Get<IRepoRegistry>()?.Open(path);
    }
}
