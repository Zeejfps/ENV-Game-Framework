using ZGF.Gui;
using ZGF.Gui.Bindings;

namespace GitGui;

internal sealed class LocalChangesHeaderActionButton : HoverableButton
{
    private const float ButtonSize = 22f;
    private const float IconSize = 13f;
    private const uint IconIdleColor = 0xFFB5B9C0;
    private const uint TransparentBg = 0x00000000u;

    public LocalChangesHeaderActionButton(string icon, Action onClick) : base(onClick)
    {
        PreferredWidth = ButtonSize;
        PreferredHeight = ButtonSize;

        var iconView = new TextView
        {
            Text = icon,
            FontFamily = LucideIcons.FontFamily,
            FontSize = IconSize,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
        };
        iconView.BindTextColor(IsHovered, h => h ? 0xFFFFFFFFu : IconIdleColor);

        var background = new RectView
        {
            BorderRadius = BorderRadiusStyle.All(3),
            Children = { iconView },
        };
        background.BindBackgroundColor(IsHovered,
            h => h ? DialogPalette.ButtonHover : TransparentBg);
        SetBackground(background);
    }
}