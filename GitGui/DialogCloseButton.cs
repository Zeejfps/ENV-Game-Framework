using ZGF.Gui;

namespace GitGui;

public sealed class DialogCloseButton : View
{
    public DialogCloseButton(Action onClick)
    {
        PreferredWidth = 28;
        PreferredHeight = 28;

        var label = new TextView
        {
            Text = "×",
            TextColor = DialogPalette.CloseTextNormal,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
        };
        var background = new RectView
        {
            BackgroundColor = DialogPalette.CloseNormal,
            BorderRadius = BorderRadiusStyle.All(4),
            Children = { label }
        };

        AddChildToSelf(background);
        Behaviors.Add(new HoverableButtonController(
            onClick,
            isHovered =>
            {
                background.BackgroundColor = isHovered ? DialogPalette.CloseHover : DialogPalette.CloseNormal;
                label.TextColor = isHovered ? DialogPalette.CloseTextHover : DialogPalette.CloseTextNormal;
            }));
    }
}