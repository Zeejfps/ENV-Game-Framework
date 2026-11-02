using System.Diagnostics;
using System.Globalization;
using ZGF.Gui.Immediate;

namespace ZGF.ILSpy;

/// <summary>An icon theme's fonts and icons merged over those of the themes it extends, before fonts are
/// looked up. Built once at the end, so an icon a parent draws in a font takes the font a child declared
/// under that id, and an icon whose font is unknown or broken keeps the one beneath it.</summary>
internal sealed class MergedIconTheme
{
    private const string IconsPrefix = "icons.";
    private const string CodepointFormat = "X4";

    private readonly Dictionary<string, IconFont> _fonts = new();
    private readonly Dictionary<string, List<IconValue>> _icons = new();

    /// <summary>Takes the fonts of <paramref name="contents"/>, under all that is added later.</summary>
    public void AddFonts(IconThemeContents contents)
    {
        foreach (var (id, font) in contents.Fonts)
            _fonts[id] = font;
    }

    /// <summary>Takes everything <paramref name="contents"/> sets, over what was added before.</summary>
    public void Add(IconThemeContents contents)
    {
        AddFonts(contents);
        foreach (var (key, value) in contents.Icons)
            LayersOf(key).Add(value);
    }

    private List<IconValue> LayersOf(string key)
    {
        if (_icons.TryGetValue(key, out var layers))
            return layers;
        layers = new List<IconValue>();
        _icons[key] = layers;
        return layers;
    }

    /// <summary>The theme the merged icons make, a key none of them draws taken from
    /// <paramref name="fallback"/>; adds what is wrong to <paramref name="errors"/>.</summary>
    public IconTheme Build(IconTheme? fallback, ICollection<ThemeError> errors)
    {
        var glyphs = new Dictionary<string, IconGlyph>();
        var values = new Dictionary<string, IconValue>();
        foreach (var (key, layers) in _icons)
            TakeTopIcon(key, layers, glyphs, values, errors);
        if (fallback == null)
            return new IconTheme(new IconSet(glyphs), ListFaces(null), values);
        foreach (var (key, value) in fallback.Values)
            values.TryAdd(key, value);
        return new IconTheme(new IconSet(glyphs, fallback.Set), ListFaces(fallback), values);
    }

    /// <summary>Takes the topmost of <paramref name="layers"/> that can be drawn, reporting each one above
    /// it whose font no theme declares.</summary>
    private void TakeTopIcon(string key, List<IconValue> layers, Dictionary<string, IconGlyph> glyphs, Dictionary<string, IconValue> values,
        ICollection<ThemeError> errors)
    {
        for (var i = layers.Count - 1; i >= 0; i--)
        {
            var value = layers[i];
            var glyph = MakeGlyph(key, value, errors);
            if (glyph == null)
                continue;
            glyphs[key] = glyph.Value;
            values[key] = value;
            return;
        }
    }

    /// <summary>What <paramref name="value"/> draws; null when its font is broken, or after reporting that
    /// no theme declares its font or that the font has no glyph for it.</summary>
    private IconGlyph? MakeGlyph(string key, IconValue value, ICollection<ThemeError> errors) => value switch
    {
        IconValue.Picture picture => IconGlyph.FromSvg(picture.Svg),
        IconValue.Glyph glyph => MakeFontGlyph(key, glyph, errors),
        _ => throw new UnreachableException(),
    };

    private IconGlyph? MakeFontGlyph(string key, IconValue.Glyph glyph, ICollection<ThemeError> errors)
    {
        if (!_fonts.TryGetValue(glyph.Font, out var font))
        {
            errors.Add(new ThemeError(glyph.File, glyph.Line, ThemeProblem.UnknownFont, IconsPrefix + key, glyph.Font));
            return null;
        }
        if (font is not IconFont.Loaded loaded)
            return null;
        if (!loaded.Characters.Contains(glyph.Codepoint))
        {
            var codepoint = glyph.Codepoint.ToString(CodepointFormat, CultureInfo.InvariantCulture);
            errors.Add(new ThemeError(glyph.File, glyph.Line, ThemeProblem.MissingGlyph, IconsPrefix + key, codepoint));
            return null;
        }
        var text = char.ConvertFromUtf32(glyph.Codepoint);
        return IconGlyph.FromFont(text, loaded.Face.Family);
    }

    /// <summary>The faces of the merged fonts that could be read, then those of <paramref name="fallback"/>
    /// in families not drawn by them.</summary>
    private List<FontFace> ListFaces(IconTheme? fallback)
    {
        var faces = _fonts.Values
            .OfType<IconFont.Loaded>()
            .Select(static f => f.Face)
            .ToList();
        if (fallback == null)
            return faces;
        var families = faces
            .Select(static f => f.Family)
            .ToHashSet();
        var inherited = fallback.Fonts
            .Where(f => !families.Contains(f.Family));
        faces.AddRange(inherited);
        return faces;
    }
}
