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
/// it extends, its palette and its <c>ui</c>, <c>code</c> and <c>icons</c> colors by key. Comments and
/// trailing commas are allowed. Reading goes on past a bad key or value, leaving it out, and lists what
/// was wrong in <see cref="Errors"/>; a file that is no JSON object at all is <see cref="IsUnreadable"/>.</summary>
public sealed class ThemeFile
{
    public const string Extension = ".json";
    public const char ColorPrefix = '#';

    private const string SchemaKey = "$schema";
    private const string NameKey = "name";
    private const string TypeKey = "type";
    private const string ExtendsKey = "extends";
    private const string PaletteKey = "palette";
    private const string UiKey = "ui";
    private const string CodeKey = "code";
    private const string IconsKey = "icons";
    private const string DarkType = "dark";
    private const string LightType = "light";
    private const string TransparentBlack = "#00000000";

    private static readonly byte[] Utf8Bom = [0xEF, 0xBB, 0xBF];

    private static readonly JsonReaderOptions Options = new()
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private static readonly HashSet<string> UiKeys = ListKeys(UiTheme.ColorKeys);
    private static readonly HashSet<string> CodeKeys = ListKeys(CodeViewPalette.ColorKeys);
    private static readonly HashSet<string> IconKeys = ListKeys(NodeIconPalette.ColorKeys);

    private readonly Dictionary<string, ThemeValue> _palette = new();
    private readonly Dictionary<string, ThemeValue> _ui = new();
    private readonly Dictionary<string, ThemeValue> _code = new();
    private readonly Dictionary<string, ThemeValue> _icons = new();
    private readonly List<ThemeError> _errors = new();
    private List<int> _lineStarts = new();

    private ThemeFile(string id, string file)
    {
        Id = id;
        File = file;
        Name = ThemeName.Plain(id);
    }

    /// <summary>The file's name without its extension, which settings and <see cref="Extends"/> know it by.</summary>
    public string Id { get; }

    /// <summary>Where the file is, as errors name it.</summary>
    public string File { get; }

    public ThemeName Name { get; private set; }

    /// <summary>The theme's type, or null to take that of the theme it extends.</summary>
    public ThemeType? Type { get; private set; }

    /// <summary>The id of the theme this one builds on, or null.</summary>
    public string? Extends { get; private set; }

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

    public IReadOnlyList<ThemeError> Errors => _errors;

    /// <summary>True when the file could not be read as a JSON object, so nothing in it counts.</summary>
    public bool IsUnreadable { get; private set; }

    /// <summary>Reads the theme file at <paramref name="path"/>; a file that cannot be read is
    /// <see cref="IsUnreadable"/>.</summary>
    public static ThemeFile ReadFile(string path)
    {
        var id = Path.GetFileNameWithoutExtension(path);
        try
        {
            var json = System.IO.File.ReadAllBytes(path);
            return Read(id, path, json);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return Unreadable(id, path, e.Message);
        }
    }

    /// <summary>Reads the theme <paramref name="id"/> from <paramref name="json"/>, the content of
    /// <paramref name="file"/>.</summary>
    public static ThemeFile Read(string id, string file, byte[] json)
    {
        var theme = new ThemeFile(id, file);
        var content = SkipBom(json);
        theme._lineStarts = FindLineStarts(content);
        theme.ReadDocument(content);
        return theme;
    }

    private static ThemeFile Unreadable(string id, string file, string reason)
    {
        var theme = new ThemeFile(id, file);
        theme.MarkUnreadable(0, ThemeProblem.Unreadable, reason);
        return theme;
    }

    private static ReadOnlySpan<byte> SkipBom(byte[] json)
    {
        var content = json.AsSpan();
        if (content.StartsWith(Utf8Bom))
            return content.Slice(Utf8Bom.Length);
        return content;
    }

    private static List<int> FindLineStarts(ReadOnlySpan<byte> content)
    {
        var starts = new List<int> { 0 };
        for (var i = 0; i < content.Length; i++)
        {
            if (content[i] == (byte)'\n')
                starts.Add(i + 1);
        }
        return starts;
    }

    private void ReadDocument(ReadOnlySpan<byte> content)
    {
        var reader = new Utf8JsonReader(content, Options);
        try
        {
            ReadRoot(ref reader);
        }
        catch (JsonException e)
        {
            var lineFromZero = e.LineNumber ?? 0;
            MarkUnreadable((int)lineFromZero + 1, ThemeProblem.InvalidJson, e.Message);
        }
        catch (InvalidOperationException e)
        {
            MarkUnreadable(LineOf(reader), ThemeProblem.InvalidJson, e.Message);
        }
    }

    private void ReadRoot(ref Utf8JsonReader reader)
    {
        reader.Read();
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            MarkUnreadable(LineOf(reader), ThemeProblem.RootNotAnObject);
            return;
        }
        while (ReadKey(ref reader, out var key, out var line))
            ReadTopLevel(ref reader, key, line);
        reader.Read();
    }

    /// <summary>Moves past the next key of the object the reader is in, onto its value. False at the
    /// object's end.</summary>
    private bool ReadKey(ref Utf8JsonReader reader, out string key, out int line)
    {
        reader.Read();
        key = "";
        line = 0;
        if (reader.TokenType == JsonTokenType.EndObject)
            return false;
        key = reader.GetString()!;
        line = LineOf(reader);
        reader.Read();
        return true;
    }

    private void ReadTopLevel(ref Utf8JsonReader reader, string key, int line)
    {
        switch (key)
        {
            case SchemaKey:
                reader.Skip();
                break;
            case NameKey:
                ReadName(ref reader, line);
                break;
            case TypeKey:
                ReadType(ref reader, line);
                break;
            case ExtendsKey:
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
                AddError(line, ThemeProblem.UnknownKey, key);
                reader.Skip();
                break;
        }
    }

    private void ReadName(ref Utf8JsonReader reader, int line)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            Name = ThemeName.Plain(reader.GetString()!);
            return;
        }
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            AddError(line, ThemeProblem.InvalidName);
            reader.Skip();
            return;
        }
        var byTag = ReadNamesByTag(ref reader);
        Name = ThemeName.PerLanguage(byTag, Id);
    }

    /// <summary>The names by language tag, tags compared ignoring case; a tag given again is reported
    /// and its first name kept.</summary>
    private Dictionary<string, string> ReadNamesByTag(ref Utf8JsonReader reader)
    {
        var byTag = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        while (ReadKey(ref reader, out var tag, out var line))
        {
            var key = NameKey + "." + tag;
            var name = ReadString(ref reader, key, line);
            if (name == null)
                continue;
            if (!byTag.TryAdd(tag, name))
                AddError(line, ThemeProblem.DuplicateLanguage, key);
        }
        return byTag;
    }

    private void ReadType(ref Utf8JsonReader reader, int line)
    {
        var type = ReadString(ref reader, TypeKey, line);
        if (type == null)
            return;
        Type = ParseType(type);
        if (Type == null)
            AddError(line, ThemeProblem.UnknownType, type);
    }

    private static ThemeType? ParseType(string type) => type switch
    {
        DarkType => ThemeType.Dark,
        LightType => ThemeType.Light,
        _ => null,
    };

    private void ReadExtends(ref Utf8JsonReader reader, int line)
    {
        var parent = ReadString(ref reader, ExtendsKey, line);
        if (parent == null)
            return;
        Extends = parent;
        ExtendsLine = line;
    }

    /// <summary>Reads the palette, whose colors must be written out, not named.</summary>
    private void ReadPalette(ref Utf8JsonReader reader, int line)
    {
        if (!IsObject(ref reader, PaletteKey, line))
            return;
        while (ReadKey(ref reader, out var name, out var nameLine))
        {
            var key = PaletteKey + "." + name;
            var color = ReadString(ref reader, key, nameLine);
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
        if (!IsObject(ref reader, section, line))
            return;
        while (ReadKey(ref reader, out var name, out var nameLine))
            ReadColor(ref reader, section, name, nameLine, colors, keys);
    }

    private void ReadColor(ref Utf8JsonReader reader, string section, string name, int line, Dictionary<string, ThemeValue> colors, HashSet<string> keys)
    {
        var key = section + "." + name;
        if (!keys.Contains(name))
        {
            AddError(line, ThemeProblem.UnknownKey, key);
            reader.Skip();
            return;
        }
        var text = ReadString(ref reader, key, line);
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
            AddError(line, ThemeProblem.NotAColor, key, color);
            return false;
        }
        if (color == TransparentBlack)
        {
            AddError(line, ThemeProblem.TransparentBlack, key);
            return false;
        }
        return true;
    }

    private bool IsObject(ref Utf8JsonReader reader, string key, int line)
    {
        if (reader.TokenType == JsonTokenType.StartObject)
            return true;
        AddError(line, ThemeProblem.NotAnObject, key);
        reader.Skip();
        return false;
    }

    /// <summary>The string the reader is on, or null after reporting that it is something else.</summary>
    private string? ReadString(ref Utf8JsonReader reader, string key, int line)
    {
        if (reader.TokenType == JsonTokenType.String)
            return reader.GetString();
        AddError(line, ThemeProblem.NotAString, key);
        reader.Skip();
        return null;
    }

    private int LineOf(Utf8JsonReader reader)
    {
        var offset = (int)reader.TokenStartIndex;
        var index = _lineStarts.BinarySearch(offset);
        if (index >= 0)
            return index + 1;
        return ~index;
    }

    private void MarkUnreadable(int line, ThemeProblem problem, string detail = "")
    {
        IsUnreadable = true;
        AddError(line, problem, "", detail);
    }

    private void AddError(int line, ThemeProblem problem, string subject = "", string detail = "")
    {
        _errors.Add(new ThemeError(File, line, problem, subject, detail));
    }

    private static HashSet<string> ListKeys(IReadOnlyList<ThemeColorKey> keys)
    {
        return keys
            .Select(static k => k.Key)
            .ToHashSet();
    }
}
