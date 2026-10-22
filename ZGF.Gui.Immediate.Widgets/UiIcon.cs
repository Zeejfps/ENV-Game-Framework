namespace ZGF.Gui.Immediate.Widgets;

/// <summary>A glyph shown as an icon. A color of 0 takes the theme's muted text color; a null
/// <see cref="Family"/> draws it in the default font. A nonzero <see cref="Background"/> draws it
/// on a rounded badge of that color. <see cref="Overlay"/> is a small glyph over its bottom-right
/// corner — a lock on a private member — in <see cref="OverlayFamily"/>, or <see cref="Family"/>
/// when null.</summary>
public readonly record struct UiIcon(
    string Glyph, string? Family = null, uint Color = 0, uint Background = 0, string? Overlay = null, uint OverlayColor = 0, string? OverlayFamily = null);

public static partial class UiWidgets
{
    private const float BadgePaddingX = 2f;

    /// <summary>Records <paramref name="icon"/> centered in a slot at least
    /// <see cref="UiTheme.IconSize"/> wide that fills its parent's height.</summary>
    public static void Icon(this Ui ui, in UiIcon icon)
    {
        var theme = ui.Theme;
        using (ui.Box(new Box { Width = Sizing.Fit(theme.IconSize), Height = Sizing.Grow(), AlignX = Align.Center, AlignY = Align.Center }))
        using (ui.Box())
        {
            ui.Text(icon.Glyph, icon.Background != 0
                    ? new Box
                    {
                        Width = Sizing.Fit(theme.IconSize),
                        Height = theme.IconSize,
                        Padding = new Spacing(BadgePaddingX, 0f),
                        Background = icon.Background,
                        Radius = theme.RadiusSmall,
                    }
                    : default,
                color: icon.Color != 0 ? icon.Color : theme.TextMuted, size: icon.Background != 0 ? null : theme.IconSize, align: Align.Center,
                family: icon.Family);
            if (icon.Overlay != null)
            {
                var overlay = theme.IconOverlaySize;
                using (ui.Float(1f, 1f, -overlay * 0.75f, -overlay * 0.75f, new Box { Width = overlay, Height = overlay }, z: 1, clipped: true))
                    ui.Text(icon.Overlay, new Box { Width = overlay, Height = overlay },
                        color: icon.OverlayColor != 0 ? icon.OverlayColor : theme.TextMuted, size: overlay, align: Align.Center,
                        family: icon.OverlayFamily ?? icon.Family);
            }
        }
    }
}
