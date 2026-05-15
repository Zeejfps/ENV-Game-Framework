using ZGF.Gui;

namespace GitGui;

public sealed class AddRepoButton : View
{
    public AddRepoButton()
    {
        PreferredHeight = 30;

        var background = new RectView
        {
            BackgroundColor = DialogPalette.ButtonNormal,
            BorderColor = BorderColorStyle.All(DialogPalette.ButtonBorder),
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
        AddChildToSelf(background);
        Behaviors.Add(new HoverableButtonController(
            () => Context?.Get<IMessageBus>()?.Broadcast<AddRepoMessage>(),
            isHovered =>
            {
                background.BackgroundColor = isHovered ? DialogPalette.ButtonHover : DialogPalette.ButtonNormal;
                background.BorderColor = BorderColorStyle.All(
                    isHovered ? DialogPalette.ButtonBorderHover : DialogPalette.ButtonBorder);
            }));
    }
}