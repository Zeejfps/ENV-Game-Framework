using ZGF.Desktop;

namespace ZGF.Gui.Immediate.Widgets;

public static partial class UiWidgets
{
    private const int LinkButtonSalt = 0x4C4E4B42;

    /// <summary>
    /// A <see cref="SelectableText"/> that leads somewhere, in the accent color unless
    /// <paramref name="color"/> says otherwise: while its element <paramref name="i"/> is hovered,
    /// <paramref name="icon"/> shows after the text as a button, named by <paramref name="tooltip"/>,
    /// that follows it, as Ctrl+click on the text does. Returns true on the frame it is followed.
    /// </summary>
    public static bool SelectableLink(this Ui ui, Interaction i, string text, TextSelection selection, ref float shift, bool active, UiIcon icon,
        string tooltip, uint? color = null, float? size = null, string? family = null, TextOverflow overflow = TextOverflow.Ellipsis)
    {
        var theme = ui.Theme;
        var following = (ui.Input.Modifiers & KeyModifiers.Control) != 0;
        var button = ui.Interact(HashCode.Combine(i.Id, LinkButtonSalt));
        if (i.Hovered)
            ui.SetCursor(following ? MouseCursor.Hand : MouseCursor.Text);
        if (button.Hovered)
            ui.SetCursor(MouseCursor.Hand);

        ui.SelectableText(i, text, selection, ref shift, active, color ?? theme.AccentText, size, family, overflow);
        if (i.Hovered || button.Hovered || button.Held)
        {
            using (ui.Box(button.Id, new Box { Width = Sizing.Fit(theme.IconSize), Height = Sizing.Grow(), AlignX = Align.Center, AlignY = Align.Center }))
                ui.Glyph(icon.Glyph, button.Hovered ? theme.AccentText : theme.TextMuted, theme.FontSizeSmall);
            ui.Tooltip(button.Id, tooltip);
        }
        return button.Clicked || i.Clicked && following;
    }

    /// <summary>Whether the pointer is over the icon a <see cref="SelectableLink"/> in element
    /// <paramref name="i"/> shows, which takes the hover from <paramref name="i"/> itself — for a
    /// row that stays highlighted while any part of it is hovered.</summary>
    public static bool IsLinkIconHovered(this Ui ui, Interaction i) => ui.IsHovered(HashCode.Combine(i.Id, LinkButtonSalt));
}
