using System.Text.Json;
using ZGF.Gui.Immediate;

namespace ZGF.ILSpy;

/// <summary>Whether a theme is drawn dark or light. A theme that extends no other takes the code and
/// icon colors of the built-in theme of its type.</summary>
public enum ThemeType
{
    Dark,
    Light,
}

/// <summary>A color as a theme file writes it, a <c>#RRGGBB</c> or <c>#RRGGBBAA</c> color or a palette
/// name, with the file and line it is written on.</summary>
public readonly record struct ThemeValue(string Text, string File, int Line)
{
    public bool IsPaletteName => !Text.StartsWith(ThemeFile.ColorPrefix);
}

/// <summary>One theme file as it is written, without the themes it extends: its name and type, the theme
/// it extends, its palette and its <c>ui</c>, <c>code</c> and <c>icons</c> colors by key, read as
/// <see cref="ThemeJson"/> reads, leaving out a bad key or value.</summary>
public sealed class ThemeFile : IThemeFile<ThemeFile>
{
    public const string Extension = ".json";
    public const char ColorPrefix = '#';

    private const string TypeKey = "type";
    private const string PaletteKey = "palette";
    private const string UiKey = "ui";
    private const string CodeKey = "code";
    private const string IconsKey = "icons";
    private const string DarkType = "dark";
    private const string LightType = "light";
    private const string TransparentBlack = "#00000000";

    private static readonly HashSet<string> UiKeys = ListKeys(UiTheme.ColorKeys);
    private static readonly HashSet<string> CodeKeys = ListKeys(CodeViewPalette.ColorKeys);
    private static readonly HashSet<string> IconKeys = ListKeys(NodeIconPalette.ColorKeys);

    private readonly ThemeJson _json;
    private readonly Dictionary<string, ThemeValue> _palette = new();
    private readonly Dictionary<string, ThemeValue> _ui = new();
    private readonly Dictionary<string, ThemeValue> _code = new();
    private readonly Dictionary<string, ThemeValue> _icons = new();

    private ThemeFile(ThemeId<ThemeFile> id, string file)
    {
        Id = id;
        _json = new ThemeJson(file);
        Name = ThemeName.Plain(id.Text);
    }

    /// <summary>The file's name without its extension, which settings and <see cref="Extends"/> know it by.</summary>
    public ThemeId<ThemeFile> Id { get; }

    /// <summary>Where the file is, as errors name it.</summary>
    public string File => _json.File;

    public ThemeName Name { get; private set; }

    /// <summary>The theme's type, or null to take that of the theme it extends.</summary>
    public ThemeType? Type { get; private set; }

    /// <summary>The id of the theme this one builds on, or null.</summary>
    public ThemeId<ThemeFile>? Extends { get; private set; }

    /// <summary>The line <see cref="Extends"/> is written on.</summary>
    public int ExtendsLine { get; private set; }

    /// <summary>The named colors the other sections can use, by name.</summary>
    public IReadOnlyDictionary<string, ThemeValue> Palette => _palette;

    /// <summary>The colors of <see cref="UiTheme"/>, by key.</summary>
    public IReadOnlyDictionary<string, ThemeValue> Ui => _ui;

    /// <summary>The colors of <see cref="CodeViewPalette"/>, by key.</summary>
    public IReadOnlyDictionary<string, ThemeValue> Code => _code;

    /// <summary>The colors of <see cref="NodeIconPalette"/>, by key.</summary>
    public IReadOnlyDictionary<string, ThemeValue> Icons => _icons;

    public IReadOnlyList<ThemeError> Errors => _json.Errors;

    /// <summary>True when the file could not be read as a JSON object, so nothing in it counts.</summary>
    public bool IsUnreadable => _json.IsUnreadable;

    /// <summary>Reads the theme <paramref name="id"/> from the file at <paramref name="path"/>; a file that
    /// cannot be read is <see cref="IsUnreadable"/>.</summary>
    public static ThemeFile ReadFile(ThemeId<ThemeFile> id, string path)
    {
        var theme = new ThemeFile(id, path);
        var json = theme._json.ReadBytes(path);
        if (json != null)
            theme._json.Read(json, theme.ReadTopLevel);
        return theme;
    }

    /// <summary>Reads the theme <paramref name="id"/> from <paramref name="json"/>, the content of
    /// <paramref name="file"/>.</summary>
    public static ThemeFile Read(ThemeId<ThemeFile> id, string file, byte[] json)
    {
        var theme = new ThemeFile(id, file);
        theme._json.Read(json, theme.ReadTopLevel);
        return theme;
    }

    private void ReadTopLevel(ref Utf8JsonReader reader, string key, int line)
    {
        switch (key)
        {
            case ThemeJson.SchemaKey:
                reader.Skip();
                break;
            case ThemeJson.NameKey:
                ReadName(ref reader, line);
                break;
            case TypeKey:
                ReadType(ref reader, line);
                break;
            case ThemeJson.ExtendsKey:
                ReadExtends(ref reader, line);
                break;
            case PaletteKey:
                ReadPalette(ref reader, line);
                break;
            case UiKey:
                ReadColors(ref reader, key, line, _ui, UiKeys);
                break;
            case CodeKey:
                ReadColors(ref reader, key, line, _code, CodeKeys);
                break;
            case IconsKey:
                ReadColors(ref reader, key, line, _icons, IconKeys);
                break;
            default:
                _json.SkipUnknown(ref reader, key, line);
                break;
        }
    }

    private void ReadName(ref Utf8JsonReader reader, int line)
    {
        var name = _json.ReadName(ref reader, line, Id.Text);
        if (name != null)
            Name = name;
    }

    private void ReadType(ref Utf8JsonReader reader, int line)
    {
        var type = _json.ReadString(ref reader, TypeKey, line);
        if (type == null)
            return;
        Type = ParseType(type);
        if (Type == null)
            _json.AddError(line, ThemeProblem.UnknownType, type);
    }

    private static ThemeType? ParseType(string type) => type switch
    {
        DarkType => ThemeType.Dark,
        LightType => ThemeType.Light,
        _ => null,
    };

    private void ReadExtends(ref Utf8JsonReader reader, int line)
    {
        var parent = _json.ReadString(ref reader, ThemeJson.ExtendsKey, line);
        if (parent == null)
            return;
        var id = ThemeId<ThemeFile>.Parse(parent);
        if (id == null)
        {
            _json.AddError(line, ThemeProblem.UnknownParent, parent);
            return;
        }
        Extends = id;
        ExtendsLine = line;
    }

    /// <summary>Reads the palette, whose colors must be written out, not named.</summary>
    private void ReadPalette(ref Utf8JsonReader reader, int line)
    {
        if (!_json.IsObject(ref reader, PaletteKey, line))
            return;
        while (_json.ReadKey(ref reader, out var name, out var nameLine))
        {
            var key = PaletteKey + "." + name;
            var color = _json.ReadString(ref reader, key, nameLine);
            if (color == null)
                continue;
            if (!IsValidColor(key, color, nameLine))
                continue;
            _palette[name] = new ThemeValue(color, File, nameLine);
        }
    }

    /// <summary>Reads a section of colors whose keys are <paramref name="keys"/>, each a color or a palette name.</summary>
    private void ReadColors(ref Utf8JsonReader reader, string section, int line, Dictionary<string, ThemeValue> colors, HashSet<string> keys)
    {
        if (!_json.IsObject(ref reader, section, line))
            return;
        while (_json.ReadKey(ref reader, out var name, out var nameLine))
            ReadColor(ref reader, section, name, nameLine, colors, keys);
    }

    private void ReadColor(ref Utf8JsonReader reader, string section, string name, int line, Dictionary<string, ThemeValue> colors, HashSet<string> keys)
    {
        var key = section + "." + name;
        if (!keys.Contains(name))
        {
            _json.SkipUnknown(ref reader, key, line);
            return;
        }
        var text = _json.ReadString(ref reader, key, line);
        if (text == null)
            return;
        var value = new ThemeValue(text, File, line);
        if (!value.IsPaletteName && !IsValidColor(key, text, line))
            return;
        colors[name] = value;
    }

    /// <summary>True when <paramref name="color"/> is a color a theme can hold; else reports why not.</summary>
    private bool IsValidColor(string key, string color, int line)
    {
        if (!UiColor.TryParse(color, out _))
        {
            _json.AddError(line, ThemeProblem.NotAColor, key, color);
            return false;
        }
        if (color == TransparentBlack)
        {
            _json.AddError(line, ThemeProblem.TransparentBlack, key);
            return false;
        }
        return true;
    }

    private static HashSet<string> ListKeys(IReadOnlyList<ThemeColorKey> keys)
    {
        return keys
            .Select(static k => k.Key)
            .ToHashSet();
    }
}
