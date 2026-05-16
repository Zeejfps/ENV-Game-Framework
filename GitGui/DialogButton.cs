using ZGF.Gui;
using ZGF.Gui.Bindings;
using ZGF.Observable;

namespace GitGui;

public sealed class DialogButton : MultiChildView
{
    public DialogButton(string label, Action onClick)
    {
        var isHovered = new State<bool>(false);

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
        background.BindBackgroundColor(isHovered,
            h => h ? DialogPalette.ButtonHover : DialogPalette.ButtonNormal);
        background.BindBorderColor(isHovered,
            h => BorderColorStyle.All(h ? DialogPalette.ButtonBorderHover : DialogPalette.ButtonBorder));
        AddChildToSelf(background);

        Behaviors.Add(new HoverableButtonController(
            onClick,
            h => isHovered.Value = h));
    }
}
