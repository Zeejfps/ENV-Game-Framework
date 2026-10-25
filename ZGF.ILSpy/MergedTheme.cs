using ZGF.Gui.Immediate;

namespace ZGF.ILSpy;

/// <summary>A theme's palette and colors merged over those of the themes it extends, before palette names
/// are looked up. Built once at the end, so a color a parent names takes the palette color a child set, a
/// color whose palette name is unknown keeps the one beneath it, and a role nobody set is derived from the
/// final base roles.</summary>
internal sealed class MergedTheme
{
    private const string UiSection = "ui";
    private const string CodeSection = "code";
    private const string IconsSection = "icons";
    private const string KeySeparator = ", ";

    private readonly Dictionary<string, ThemeValue> _palette = new();
    private readonly Dictionary<string, List<ThemeValue>> _ui = new();
    private readonly Dictionary<string, List<ThemeValue>> _code = new();
    private readonly Dictionary<string, List<ThemeValue>> _icons = new();

    /// <summary>Takes the code and icon colors of <paramref name="file"/>, under all that is added later.</summary>
    public void AddCodeAndIcons(ThemeFile file)
    {
        Stack(_code, file.Code);
        Stack(_icons, file.Icons);
    }

    /// <summary>Takes everything <paramref name="file"/> sets, over what was added before.</summary>
    public void Add(ThemeFile file)
    {
        foreach (var (name, value) in file.Palette)
            _palette[name] = value;
        Stack(_ui, file.Ui);
        Stack(_code, file.Code);
        Stack(_icons, file.Icons);
    }

    private static void Stack(Dictionary<string, List<ThemeValue>> merged, IReadOnlyDictionary<string, ThemeValue> added)
    {
        foreach (var (key, value) in added)
            LayersOf(merged, key).Add(value);
    }

    private static List<ThemeValue> LayersOf(Dictionary<string, List<ThemeValue>> merged, string key)
    {
        if (merged.TryGetValue(key, out var layers))
            return layers;
        layers = new List<ThemeValue>();
        merged[key] = layers;
        return layers;
    }

    /// <summary>The theme the merged colors make, or null when a required color is missing; adds what
    /// is wrong to <paramref name="errors"/>, a missing color as an error of <paramref name="file"/>.</summary>
    public AppTheme? Build(string file, ICollection<ThemeError> errors)
    {
        var ui = LookUpColors(UiSection, _ui, errors);
        var code = LookUpColors(CodeSection, _code, errors);
        var icons = LookUpColors(IconsSection, _icons, errors);
        var missing = new List<string>();
        AddMissing(missing, UiSection, UiTheme.ColorKeys, ui);
        AddMissing(missing, CodeSection, CodeViewPalette.ColorKeys, code);
        AddMissing(missing, IconsSection, NodeIconPalette.ColorKeys, icons);
        if (missing.Count > 0)
        {
            errors.Add(new ThemeError(file, 0, ThemeProblem.MissingColors, Detail: string.Join(KeySeparator, missing)));
            return null;
        }
        return AppTheme.FromColors(UiTheme.FromColors(ui), CodeViewPalette.FromColors(code), NodeIconPalette.FromColors(icons));
    }

    private Dictionary<string, uint> LookUpColors(string section, Dictionary<string, List<ThemeValue>> merged, ICollection<ThemeError> errors)
    {
        var colors = new Dictionary<string, uint>();
        foreach (var (key, layers) in merged)
            LookUpTopColor(section, key, layers, colors, errors);
        return colors;
    }

    /// <summary>Takes the color of the topmost of <paramref name="layers"/> that has one, reporting each
    /// palette name above it that the palette does not have.</summary>
    private void LookUpTopColor(string section, string key, List<ThemeValue> layers, Dictionary<string, uint> colors, ICollection<ThemeError> errors)
    {
        for (var i = layers.Count - 1; i >= 0; i--)
        {
            var value = layers[i];
            if (TryLookUp(value, out var color))
            {
                colors[key] = color;
                return;
            }
            errors.Add(new ThemeError(value.File, value.Line, ThemeProblem.UnknownPaletteName, section + "." + key, value.Text));
        }
    }

    private bool TryLookUp(ThemeValue value, out uint color)
    {
        if (!value.IsPaletteName)
            return UiColor.TryParse(value.Text, out color);
        if (_palette.TryGetValue(value.Text, out var named))
            return UiColor.TryParse(named.Text, out color);
        color = 0;
        return false;
    }

    private static void AddMissing(List<string> missing, string section, IReadOnlyList<ThemeColorKey> keys, Dictionary<string, uint> colors)
    {
        var absent = keys
            .Where(static k => k.IsRequired)
            .Where(k => !colors.ContainsKey(k.Key))
            .Select(k => section + "." + k.Key);
        missing.AddRange(absent);
    }
}
