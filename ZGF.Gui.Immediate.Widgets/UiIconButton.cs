namespace ZGF.Gui.Immediate.Widgets;

/// <summary>How a <see cref="UiWidgets.Button"/> is emphasized: <see cref="Neutral"/> on the control
/// fill with an outline, <see cref="Primary"/> filled with the accent for the action a dialog or
/// screen exists for, <see cref="Ghost"/> with no fill until the pointer is over it.</summary>
public enum ButtonVariant
{
    Neutral,
    Primary,
    Ghost,
}

public static partial class UiWidgets
{
    /// <summary>A square button of <see cref="UiTheme.IconButtonSize"/> showing one icon
    /// <paramref name="glyph"/>, centered at <see cref="UiTheme.IconSize"/>, with an optional tooltip: <see cref="ButtonVariant.Ghost"/> unless
    /// <paramref name="variant"/> says otherwise. Returns true on the frame it is clicked, or activated
    /// with Enter or Space while focused. <paramref name="id"/> is scoped like any other.</summary>
    public static bool IconButton(this Ui ui, string id, IconGlyph glyph, string? tooltip = null, bool enabled = true,
        ButtonVariant variant = ButtonVariant.Ghost)
    {
        var theme = ui.Theme;
        var i = Pressable(ui, id, enabled, out var activated, out var ring);
        var (fill, border, text) = ButtonColors(theme, variant, enabled, i.Held || activated, i.Hovered);
        var size = theme.IconButtonSize;

        using (ui.Box(i.Id, new Box
        {
            Width = size,
            Height = size,
            AlignX = Align.Center,
            AlignY = Align.Center,
            Background = fill,
            BorderColor = border,
            BorderWidth = border != 0 ? theme.BorderWidth : 0f,
            Radius = theme.CornerRadius,
        }))
        {
            ui.Glyph(glyph, text, theme.IconSize, new Box { Width = Sizing.Fit(theme.IconSize), Height = theme.IconSize });
            if (ring) FocusRing(ui, theme.CornerRadius);
        }
        if (tooltip != null)
            ui.Tooltip(i.Id, tooltip);
        return i.Clicked || activated;
    }

    /// <summary>A square ghost button showing a cross, the theme's <see cref="UiTheme.TabCloseSize"/>
    /// wide unless <paramref name="size"/> is given. Returns true on the frame it is clicked with the
    /// left or middle button, or activated with Enter or Space while focused; without
    /// <paramref name="tabStop"/> it takes no focus. With <paramref name="hideIdle"/> the cross shows
    /// only while the button is hovered or focused.</summary>
    public static bool CloseButton(this Ui ui, int id, float? size = null, bool tabStop = true, bool hideIdle = false)
    {
        var theme = ui.Theme;
        var close = ui.Pressable(id, true, out var activated, out var ring, tabStop: tabStop);
        var side = size ?? theme.TabCloseSize;
        using (ui.Box(close.Id, new Box
        {
            Width = side,
            Height = side,
            Radius = theme.CornerRadius,
            Background = theme.ControlFill(close.Held, close.Hovered),
        }))
        {
            var glyph = CloseGlyphColor(theme, close.Hovered || ring, hideIdle);
            if (glyph != 0)
                ui.PaintGlyph(UiGlyph.Cross, glyph);
            if (ring) FocusRing(ui, theme.CornerRadius);
        }
        return close.Clicked || activated || close.ClickedWith(PointerButton.Middle);
    }

    private static uint CloseGlyphColor(UiTheme theme, bool lit, bool hideIdle)
    {
        if (lit) return theme.Text;
        if (hideIdle) return 0u;
        return theme.TextMuted;
    }
}
