using ZGF.Gui;
using ZGF.Gui.Bindings;
using ZGF.Observable;

namespace GitGui;

internal sealed class LocalChangesHeaderActionButton : HoverableButton
{
    private const float ButtonSize = 22f;
    private const float IconSize = 13f;
    private const uint IconDisabledColor = 0x66B5B9C0;
    private const uint TransparentBg = 0x00000000u;

    public LocalChangesHeaderActionButton(string icon, Action onClick, string? tooltip = null)
        : base(onClick, tooltip)
    {
        PreferredWidth = ButtonSize;
        PreferredHeight = ButtonSize;

        var idleIcon = new State<uint>(ThemePresets.Dark.Dialog.RowText);
        var hoverIcon = new State<uint>(ThemePresets.Dark.Text.Strong);
        var hoverBg = new State<uint>(ThemePresets.Dark.Dialog.ButtonHover);
        this.BindToTheme(t =>
        {
            idleIcon.Value = t.Dialog.RowText;
            hoverIcon.Value = t.Text.Strong;
            hoverBg.Value = t.Dialog.ButtonHover;
        });

        var iconView = new TextView
        {
            Text = icon,
            FontFamily = LucideIcons.FontFamily,
            FontSize = IconSize,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
        };
        iconView.BindTextColor(() =>
        {
            if (!IsEnabled) return IconDisabledColor;
            return IsHovered ? hoverIcon.Value : idleIcon.Value;
        });

        var background = new RectView
        {
            BorderRadius = BorderRadiusStyle.All(3),
            Children = { iconView },
        };
        background.BindBackgroundColor(() =>
            IsEnabled && IsHovered ? hoverBg.Value : TransparentBg);
        SetBackground(background);
    }
}