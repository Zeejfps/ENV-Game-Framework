using ZGF.Gui;
using ZGF.Gui.Bindings;
using ZGF.Gui.Layouts;

namespace GitGui;

public sealed class ActionButton : HoverableButton
{
    public ActionButton(string icon, string label, Action onClick) : base(onClick)
    {
        PreferredHeight = 28;

        var iconView = new TextView
        {
            Text = icon,
            FontFamily = LucideIcons.FontFamily,
            FontSize = 14,
            VerticalTextAlignment = TextAlignment.Center,
        };
        iconView.BindTextColor(IsHovered, h => h ? 0xFFFFFFFF : DialogPalette.RowText);

        var labelView = new TextView
        {
            Text = label,
            VerticalTextAlignment = TextAlignment.Center,
        };
        labelView.BindTextColor(IsHovered, h => h ? 0xFFFFFFFF : DialogPalette.RowText);

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
        background.BindBackgroundColor(IsHovered,
            h => h ? DialogPalette.ButtonHover : 0x00000000u);
        SetBackground(background);
    }
}
