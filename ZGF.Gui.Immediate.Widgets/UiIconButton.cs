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
    /// <summary>A square <see cref="ButtonVariant.Ghost"/> button of
    /// <see cref="UiTheme.IconButtonSize"/> showing one icon <paramref name="glyph"/>, centered at
    /// <see cref="UiTheme.IconSize"/> in <paramref name="family"/> (the theme's font when null), with an
    /// optional tooltip. Returns true on the frame it is clicked, or activated with Enter or Space while
    /// focused. <paramref name="id"/> is scoped like any other.</summary>
    public static bool IconButton(this Ui ui, string id, string glyph, string? family = null, string? tooltip = null, bool enabled = true)
    {
        var theme = ui.Theme;
        var i = Pressable(ui, id, enabled, out var activated, out var ring);
        var (fill, _, text) = ButtonColors(theme, ButtonVariant.Ghost, enabled, i.Held || activated, i.Hovered);
        var size = theme.IconButtonSize;

        using (ui.Box(i.Id, new Box
        {
            Width = size,
            Height = size,
            AlignX = Align.Center,
            AlignY = Align.Center,
            Background = fill,
            Radius = theme.CornerRadius,
        }))
        {
            ui.Text(glyph, new Box { Width = Sizing.Fit(theme.IconSize), Height = theme.IconSize }, color: text, size: theme.IconSize, align: Align.Center,
                family: family);
            if (ring) FocusRing(ui, theme.CornerRadius);
        }
        if (tooltip != null)
            ui.Tooltip(i.Id, tooltip);
        return i.Clicked || activated;
    }
}
