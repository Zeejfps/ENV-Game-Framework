using ZGF.Gui;

namespace GitGui;

public sealed class DialogButton : HoverableButton
{
    public DialogButton(string label, Action onClick) : base(onClick)
    {
        var background = new RectView
        {
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
        DialogPalette.BindBorderedButtonChrome(background, IsHovered);
        SetBackground(background);
    }
}
