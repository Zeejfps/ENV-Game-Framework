using ZGF.Gui.Immediate;

namespace ZGF.ILSpy;

/// <summary>How the documents draw code: the font size, null for the theme's, and the aids drawn
/// around the text.</summary>
public sealed record DisplayOptions(
    float? FontSize = null,
    bool ShowLineNumbers = true,
    bool HighlightCurrentLine = false,
    bool HighlightMatchingBrackets = true)
{
    public const float MinFontSize = 8f;
    public const float MaxFontSize = 32f;
    private const float FontSizeStep = 1f;

    public static DisplayOptions Default { get; } = new();

    /// <summary>The options the settings kept, with a font size out of range brought within it; the
    /// defaults when none were kept.</summary>
    public static DisplayOptions Restore(DisplayOptions? saved)
    {
        if (saved == null)
            return Default;
        if (saved.FontSize == null)
            return saved;
        return saved.WithFontSize(saved.FontSize.Value);
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
    /// it is in <paramref name="theme"/>.</summary>
    public DisplayOptions Zoomed(UiTheme theme, int steps)
    {
        var size = FontSizeIn(theme) + steps * FontSizeStep;
        return WithFontSize(size);
    }

    public bool CanZoomIn(UiTheme theme) => FontSizeIn(theme) < MaxFontSize;

    public bool CanZoomOut(UiTheme theme) => FontSizeIn(theme) > MinFontSize;
}
