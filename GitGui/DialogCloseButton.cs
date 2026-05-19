using ZGF.Gui;
using ZGF.Gui.Bindings;

namespace GitGui;

public sealed class DialogCloseButton : HoverableButton
{
    public DialogCloseButton(Action onClick) : base(onClick)
    {
        PreferredWidth = 28;
        PreferredHeight = 28;

        var label = new TextView
        {
            Text = "×",
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
        };
        label.BindTextColor(() =>
            IsHovered ? DialogPalette.CloseTextHover : DialogPalette.CloseTextNormal);

        var background = new RectView
        {
            BorderRadius = BorderRadiusStyle.All(4),
            Children = { label }
        };
        background.BindBackgroundColor(() =>
            IsHovered ? DialogPalette.CloseHover : DialogPalette.CloseNormal);

        SetBackground(background);
    }
}
