using ZGF.Gui;

namespace GitGui;

public sealed class DialogButton : View
{
    public DialogButton(string label, Action onClick)
    {
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
                    Text = label,
                    TextColor = 0xFFFFFFFF,
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center,
                }
            }
        };
        AddChildToSelf(background);
        Behaviors.Add(new HoverableButtonController(
            onClick,
            isHovered =>
            {
                background.BackgroundColor = isHovered ? DialogPalette.ButtonHover : DialogPalette.ButtonNormal;
                background.BorderColor = BorderColorStyle.All(
                    isHovered ? DialogPalette.ButtonBorderHover : DialogPalette.ButtonBorder);
            }));
    }
}