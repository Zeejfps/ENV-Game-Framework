using ZGF.Svg;

namespace ZGF.Gui.Immediate;

/// <summary>What an icon draws: a glyph of a font, <see cref="Text"/> in <see cref="Family"/>, or the
/// SVG picture <see cref="Svg"/>, tinted through <c>currentColor</c> in the color it is drawn in. The
/// default draws nothing.</summary>
public readonly record struct IconGlyph
{
    private IconGlyph(string? text, string? family, SvgDocument? svg)
    {
        Text = text;
        Family = family;
        Svg = svg;
    }

    /// <summary>The font glyph's text; null for a picture.</summary>
    public string? Text { get; }

    /// <summary>The font family <see cref="Text"/> is drawn in; null for the theme's font.</summary>
    public string? Family { get; }

    /// <summary>The picture; null for a font glyph.</summary>
    public SvgDocument? Svg { get; }

    /// <summary><paramref name="text"/> drawn in <paramref name="family"/>, or in the theme's font when null.</summary>
    public static IconGlyph FromFont(string text, string? family = null) => new(text, family, null);

    public static IconGlyph FromSvg(SvgDocument svg) => new(null, null, svg);
}
