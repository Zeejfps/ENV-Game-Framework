namespace ZGF.Gui.Immediate.Widgets;

/// <summary>A glyph shown as an icon. A color of 0 takes the theme's muted text color. A nonzero
/// <see cref="Background"/> draws it on a rounded badge of that color. <see cref="Overlay"/> is a small
/// glyph over its bottom-right corner — a lock on a private member.</summary>
public readonly record struct UiIcon(IconGlyph Glyph, uint Color = 0, uint Background = 0, IconGlyph? Overlay = null, uint OverlayColor = 0);

public static partial class UiWidgets
{
    private const float BadgePaddingX = 2f;
    private const float OverlayInset = 0.75f;

    /// <summary>Records <paramref name="icon"/> centered in a slot at least
    /// <see cref="UiTheme.IconSize"/> wide that fills its parent's height.</summary>
    public static void Icon(this Ui ui, in UiIcon icon)
    {
        var theme = ui.Theme;
        using (ui.Box(new Box { Width = Sizing.Fit(theme.IconSize), Height = Sizing.Grow(), AlignX = Align.Center, AlignY = Align.Center }))
        using (ui.Box())
        {
            var color = icon.Color != 0 ? icon.Color : theme.TextMuted;
            if (icon.Background != 0)
                ui.Glyph(icon.Glyph, color, box: BadgeBox(theme, icon.Background));
            else
                ui.Glyph(icon.Glyph, color, theme.IconSize);
            if (icon.Overlay != null)
                IconOverlay(ui, icon.Overlay.Value, icon.OverlayColor);
        }
    }

    private static Box BadgeBox(UiTheme theme, uint background) => new()
    {
        Width = Sizing.Fit(theme.IconSize),
        Height = theme.IconSize,
        Padding = new Spacing(BadgePaddingX, 0f),
        Background = background,
        Radius = theme.RadiusSmall,
    };

    private static void IconOverlay(Ui ui, IconGlyph overlay, uint color)
    {
        var theme = ui.Theme;
        var size = theme.IconOverlaySize;
        var shown = color != 0 ? color : theme.TextMuted;
        using (ui.Float(1f, 1f, -size * OverlayInset, -size * OverlayInset, new Box { Width = size, Height = size }, z: 1, clipped: true))
            ui.Glyph(overlay, shown, size, new Box { Width = size, Height = size });
    }
}
