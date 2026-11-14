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
        DrawIconButton(ui, id, state, new IconButtonFace(glyph, variant), tooltip, SemanticStates.None);
        return state.Activated;
    }

    /// <summary>An <see cref="IconButton"/> that toggles <paramref name="on"/> when clicked, or activated with Enter
    /// or Space, and shows it: on the selection fill while on. Returns true on the frame it toggles.</summary>
    public static bool IconToggle(this Ui ui, string id, IconGlyph glyph, ref bool on, string? tooltip = null, bool enabled = true)
    {
        var state = ui.ButtonBehavior(id, enabled);
        if (state.Activated)
            on = !on;
        var face = new IconButtonFace(glyph, ButtonVariant.Ghost, Checked: on);
        DrawIconButton(ui, id, state, face, tooltip, on ? SemanticStates.Checked : SemanticStates.None);
        return state.Activated;
    }

    private static void DrawIconButton(Ui ui, string id, in ButtonState state, in IconButtonFace face, string? tooltip, SemanticStates states)
    {
        var painter = ui.Theme.IconButtonPainter;
        if (painter != null)
            painter(ui, state, face);
        else
            ui.PaintIconButton(state, face);
        ui.Semantics(state.Id, SemanticRole.Button, tooltip ?? id, ButtonStates(state.Availability) | states);
        if (tooltip != null)
            ui.Tooltip(state.Id, tooltip);
    }

    /// <summary>The stock look of an <see cref="IconButton"/>: <see cref="UiTheme.IconButtonSize"/>
    /// square on its variant's colors, the glyph in its text color; a checked one on the selection fill. In a
    /// <see cref="JoinedGroup"/> it is square-cornered and unoutlined, divided from the control before it — for a
    /// painter of an app's own to fall back to.</summary>
    public static void PaintIconButton(this Ui ui, in ButtonState state, in IconButtonFace face)
    {
        var theme = ui.Theme;
        var (fill, border, text) = ButtonColors(theme, face.Variant, state);
        if (face.Checked && state.Availability != ButtonAvailability.Disabled)
            (fill, text) = (state.Down ? theme.ControlPressed : theme.Selection, theme.SelectionText);
        var slot = ui.TakeJoinedSlot();
        var size = theme.IconButtonSize;
        var radius = slot == null ? theme.CornerRadius : 0f;
        if (slot != null)
            border = 0u;

        using (ui.Box(state.Id, new Box
        {
            Width = size,
            Height = slot != null ? Sizing.Grow() : size,
            AlignX = Align.Center,
            AlignY = Align.Center,
            Background = fill,
            BorderColor = border,
            BorderWidth = border != 0 ? theme.BorderWidth : 0f,
            Radius = radius,
        }))
        {
            ui.PaintJoinedDivider(slot);
            ui.Glyph(face.Glyph, text, theme.IconSize, new Box { Width = Sizing.Fit(theme.IconSize), Height = theme.IconSize });
            if (state.FocusVisible) FocusRing(ui, radius);
        }
    }

    /// <summary>A square ghost button showing a cross, the theme's <see cref="UiTheme.TabCloseSize"/>
    /// wide unless <paramref name="size"/> is given. Returns true on the frame it is clicked with the
    /// left or middle button, or activated with Enter or Space while focused; without
    /// <paramref name="tabStop"/> it takes no focus. With <paramref name="hideIdle"/> the cross shows
    /// only while the button is hovered or focused.</summary>
    public static bool CloseButton(this Ui ui, int id, float? size = null, bool tabStop = true, bool hideIdle = false)
    {
        var state = ui.CloseButtonBehavior(id, tabStop);
        ui.Semantics(state.Id, SemanticRole.Button, "Close");
        ui.PaintCloseButton(state, size, hideIdle);
        return state.Activated;
    }

    /// <summary>What a <see cref="CloseButton"/> does, without drawing it: it is
    /// <see cref="ButtonState.Activated"/> when clicked with the left or middle button, or with Enter or Space while
    /// focused, and <see cref="ButtonState.Down"/> while the pointer holds it; without <paramref name="tabStop"/> it
    /// takes no focus. Record its element with the returned <see cref="ButtonState.Id"/> this frame, as
    /// <see cref="PaintCloseButton"/> does.</summary>
    public static ButtonState CloseButtonBehavior(this Ui ui, int id, bool tabStop = true)
    {
        var close = ui.Pressable(id, true, out var activated, out var ring, tabStop: tabStop);
        var clicked = close.Clicked || activated || close.ClickedWith(PointerButton.Middle);
        return new ButtonState(close.Id, ButtonAvailability.Enabled, close.Hovered, close.Held, ring, clicked);
    }

    /// <summary>The stock look of a <see cref="CloseButton"/>: a square of the theme's
    /// <see cref="UiTheme.TabCloseSize"/>, unless <paramref name="size"/> is given, filled while hovered or held, its
    /// cross shown only while lit when <paramref name="hideIdle"/> — for a painter of an app's own to fall back to.</summary>
    public static void PaintCloseButton(this Ui ui, in ButtonState state, float? size = null, bool hideIdle = false)
    {
        var theme = ui.Theme;
        var side = size ?? theme.TabCloseSize;
        using (ui.Box(state.Id, new Box
        {
            Width = side,
            Height = side,
            Radius = theme.CornerRadius,
            Background = theme.ControlFill(state.Down, state.Hovered),
        }))
        {
            var glyph = CloseGlyphColor(theme, state.Hovered || state.FocusVisible, hideIdle);
            if (glyph != 0)
                ui.PaintGlyph(UiGlyph.Cross, glyph);
            if (state.FocusVisible) FocusRing(ui, theme.CornerRadius);
        }
    }

    private static uint CloseGlyphColor(UiTheme theme, bool lit, bool hideIdle)
    {
        if (lit) return theme.Text;
        if (hideIdle) return 0u;
        return theme.TextMuted;
    }
}
