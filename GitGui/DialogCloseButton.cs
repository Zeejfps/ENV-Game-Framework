using ZGF.Gui;
using ZGF.Gui.Bindings;
using ZGF.Observable;

namespace GitGui;

public sealed class DialogCloseButton : MultiChildView
{
    public DialogCloseButton(Action onClick)
    {
        PreferredWidth = 28;
        PreferredHeight = 28;

        var isHovered = new State<bool>(false);

        var label = new TextView
        {
            Text = "×",
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
        };
        label.BindTextColor(isHovered,
            h => h ? DialogPalette.CloseTextHover : DialogPalette.CloseTextNormal);

        var background = new RectView
        {
            BorderRadius = BorderRadiusStyle.All(4),
            Children = { label }
        };
        background.BindBackgroundColor(isHovered,
            h => h ? DialogPalette.CloseHover : DialogPalette.CloseNormal);

        AddChildToSelf(background);

        Behaviors.Add(new HoverableButtonController(
            onClick,
            h => isHovered.Value = h));
    }
}
