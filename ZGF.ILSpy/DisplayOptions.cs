using ZGF.Gui.Immediate;

namespace ZGF.ILSpy;

/// <summary>How the app is drawn: the scale of the whole interface, 1 for 100%, and whether the window
/// keeps the system's title bar, with the menus in a bar of their own, rather than the menus drawn in
/// the title bar where the platform allows; how the documents draw code: its font, null for the bundled
/// one, its size, null for the theme's, and the aids drawn around it; and how the tree writes metadata
/// tokens.</summary>
public sealed record DisplayOptions(
    float? FontSize = null,
    bool ShowLineNumbers = true,
    bool HighlightCurrentLine = false,
    bool HighlightMatchingBrackets = true,
    float UiScale = 1f,
    CodeFont? Font = null,
    TokenFormat TreeTokens = TokenFormat.Hidden,
    bool SystemTitleBar = false)
{
    public const float MinFontSize = 8f;
    public const float MaxFontSize = 32f;
    private const float FontSizeStep = 1f;

    private static readonly float[] Scales = [0.75f, 0.8f, 0.9f, 1f, 1.1f, 1.25f, 1.5f, 1.75f, 2f];

    public static DisplayOptions Default { get; } = new();

    /// <summary>The options the settings kept, with a font size out of range brought within it and
    /// the interface scale made the nearest offered; the defaults when none were kept.</summary>
    public static DisplayOptions Restore(DisplayOptions? saved)
    {
        if (saved == null)
            return Default;
        var scaled = saved.WithUiScale(saved.UiScale);
        if (scaled.FontSize == null)
            return scaled;
        return scaled.WithFontSize(scaled.FontSize.Value);
    }

    private static float NearestUiScale(float scale)
    {
        return Scales
            .MinBy(s => MathF.Abs(s - scale));
    }

    /// <summary>The size code is drawn at in <paramref name="theme"/>.</summary>
    public float FontSizeIn(UiTheme theme)
    {
        if (FontSize == null)
            return theme.MonoFontSize;
        return FontSize.Value;
    }

    public DisplayOptions WithFontSize(float size)
    {
        var clamped = Math.Clamp(size, MinFontSize, MaxFontSize);
        return this with { FontSize = clamped };
    }

    /// <summary>The font size <paramref name="steps"/> steps larger, or smaller when negative, than
    /// it is in <paramref name="theme"/>; the theme's own again once it gets back to that size.</summary>
    public DisplayOptions Zoomed(UiTheme theme, int steps)
    {
        var size = FontSizeIn(theme) + steps * FontSizeStep;
        var zoomed = WithFontSize(size);
        if (zoomed.FontSize == theme.MonoFontSize)
            return zoomed with { FontSize = null };
        return zoomed;
    }

    public bool CanZoomIn(UiTheme theme) => FontSizeIn(theme) < MaxFontSize;

    public bool CanZoomOut(UiTheme theme) => FontSizeIn(theme) > MinFontSize;

    /// <summary>The interface scales offered, smallest first.</summary>
    public static IReadOnlyList<float> UiScales => Scales;

    /// <summary>The options with the offered interface scale nearest <paramref name="scale"/>.</summary>
    public DisplayOptions WithUiScale(float scale)
    {
        var nearest = NearestUiScale(scale);
        return this with { UiScale = nearest };
    }
}
