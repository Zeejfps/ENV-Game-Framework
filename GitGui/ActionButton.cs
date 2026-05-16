using ZGF.Gui;
using ZGF.Gui.Bindings;
using ZGF.Gui.Layouts;
using ZGF.Observable;

namespace GitGui;

public sealed class ActionButton : MultiChildView
{
    public ActionButton(string icon, string label, Action onClick)
    {
        PreferredHeight = 28;

        var isHovered = new State<bool>(false);

        var iconView = new TextView
        {
            Text = icon,
            FontFamily = LucideIcons.FontFamily,
            FontSize = 14,
            VerticalTextAlignment = TextAlignment.Center,
        };
        iconView.BindTextColor(isHovered, h => h ? 0xFFFFFFFF : DialogPalette.RowText);

        var labelView = new TextView
        {
            Text = label,
            VerticalTextAlignment = TextAlignment.Center,
        };
        labelView.BindTextColor(isHovered, h => h ? 0xFFFFFFFF : DialogPalette.RowText);

        var background = new RectView
        {
            Padding = new PaddingStyle { Left = 8, Right = 8 },
            Children =
            {
                new RowView
                {
                    Gap = 6,
                    Children = { iconView, labelView },
                }
            }
        };
        background.BindBackgroundColor(isHovered,
            h => h ? DialogPalette.ButtonHover : 0x00000000u);
        AddChildToSelf(background);

        Behaviors.Add(new HoverableButtonController(onClick, h => isHovered.Value = h));
    }
}
