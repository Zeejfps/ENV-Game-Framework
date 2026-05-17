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
        iconView.BindTextColor(ComputeForeground);

        var labelView = new TextView
        {
            Text = label,
            VerticalTextAlignment = TextAlignment.Center,
        };
        labelView.BindTextColor(ComputeForeground);

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
        background.BindBackgroundColor(() =>
            IsEnabled.Value && IsHovered.Value ? DialogPalette.ButtonHover : 0x00000000u);
        SetBackground(background);
    }

    private uint ComputeForeground()
    {
        if (!IsEnabled.Value) return DialogPalette.RowTextMissing;
        return IsHovered.Value ? 0xFFFFFFFFu : DialogPalette.RowText;
    }
}
