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

    /// <summary>Takes the fonts of <paramref name="file"/>, under all that is added later.</summary>
    public void AddFonts(IconThemeFile file)
    {
        foreach (var (id, font) in file.Fonts)
            _fonts[id] = font;
    }

    /// <summary>Takes everything <paramref name="file"/> sets, over what was added before.</summary>
    public void Add(IconThemeFile file)
    {
        AddFonts(file);
        foreach (var (key, value) in file.Icons)
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
    private IconGlyph? MakeGlyph(string key, IconValue value, ICollection<ThemeError> errors)
    {
        if (value.Svg != null)
            return IconGlyph.FromSvg(value.Svg);
        if (!_fonts.TryGetValue(value.Font!, out var font))
        {
            errors.Add(new ThemeError(value.File, value.Line, ThemeProblem.UnknownFont, IconsPrefix + key, value.Font!));
            return null;
        }
        if (font.Face == null)
            return null;
        if (!font.Characters!.Contains(value.Codepoint))
        {
            var codepoint = value.Codepoint.ToString(CodepointFormat, CultureInfo.InvariantCulture);
            errors.Add(new ThemeError(value.File, value.Line, ThemeProblem.MissingGlyph, IconsPrefix + key, codepoint));
            return null;
        }
        var text = char.ConvertFromUtf32(value.Codepoint);
        return IconGlyph.FromFont(text, font.Face.Family);
    }

    /// <summary>The faces of the merged fonts that could be read, then those of <paramref name="fallback"/>
    /// in families not drawn by them.</summary>
    private List<FontFace> ListFaces(IconTheme? fallback)
    {
        var faces = _fonts.Values
            .Select(static f => f.Face)
            .OfType<FontFace>()
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
