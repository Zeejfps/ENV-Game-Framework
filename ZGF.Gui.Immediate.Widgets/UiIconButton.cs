namespace ZGF.Gui.Immediate.Widgets;

public static partial class UiWidgets
{
    /// <summary>A square button of <see cref="UiTheme.IconButtonSize"/> showing one icon
    /// <paramref name="glyph"/>, centered at <see cref="UiTheme.IconSize"/>, with an optional tooltip: <see cref="ButtonVariant.Ghost"/> unless
    /// <paramref name="variant"/> says otherwise. Returns true on the frame it is clicked, or activated
    /// with Enter or Space while focused. <paramref name="id"/> is scoped like any other. It is drawn by
    /// the theme's <see cref="UiTheme.IconButtonPainter"/>, or as <see cref="PaintIconButton"/> draws one.</summary>
    public static bool IconButton(this Ui ui, string id, IconGlyph glyph, string? tooltip = null, bool enabled = true,
        ButtonVariant variant = ButtonVariant.Ghost)
    {
        var state = ui.ButtonBehavior(id, enabled);
        var face = new IconButtonFace(glyph, variant);
        var painter = ui.Theme.IconButtonPainter;
        if (painter != null)
            painter(ui, state, face);
        else
            ui.PaintIconButton(state, face);
        if (tooltip != null)
            ui.Tooltip(state.Id, tooltip);
        return state.Activated;
    }

    /// <summary>The stock look of an <see cref="IconButton"/>: <see cref="UiTheme.IconButtonSize"/>
    /// square on its variant's colors, the glyph in its text color — for a painter of an app's own to
    /// fall back to.</summary>
    public static void PaintIconButton(this Ui ui, in ButtonState state, in IconButtonFace face)
    {
        var theme = ui.Theme;
        var (fill, border, text) = ButtonColors(theme, face.Variant, state);
        var size = theme.IconButtonSize;

        using (ui.Box(state.Id, new Box
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
            ui.Glyph(face.Glyph, text, theme.IconSize, new Box { Width = Sizing.Fit(theme.IconSize), Height = theme.IconSize });
            if (state.FocusVisible) FocusRing(ui, theme.CornerRadius);
        }
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
