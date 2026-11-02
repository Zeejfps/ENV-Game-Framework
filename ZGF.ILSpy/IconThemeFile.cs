using System.Globalization;
using System.Text;
using System.Text.Json;
using ZGF.Fonts;
using ZGF.Gui.Immediate;
using ZGF.Svg;

namespace ZGF.ILSpy;

/// <summary>A font an icon theme file declares by the path it names: loaded, or broken when it could not
/// be read or loaded, which was reported.</summary>
public abstract record IconFont
{
    private IconFont(string path)
    {
        Path = path;
    }

    public string Path { get; }

    /// <summary>The face read from the font and the characters it has glyphs for.</summary>
    public sealed record Loaded(string Path, FontFace Face, FontCharacters Characters) : IconFont(Path);

    public sealed record Broken(string Path) : IconFont(Path);
}

/// <summary>An icon as an icon theme file writes it, with the file and line it is written on: a glyph of
/// a font, or a picture read from an SVG file.</summary>
public abstract record IconValue
{
    private IconValue(string file, int line)
    {
        File = file;
        Line = line;
    }

    public string File { get; }

    public int Line { get; }

    /// <summary>The codepoint <see cref="Codepoint"/> of the font the theme declares as <see cref="Font"/>.</summary>
    public sealed record Glyph(string Font, int Codepoint, string File, int Line) : IconValue(File, Line);

    /// <summary>The picture <see cref="Svg"/> read from the file at <see cref="SvgPath"/>.</summary>
    public sealed record Picture(string SvgPath, SvgDocument Svg, string File, int Line) : IconValue(File, Line);
}

/// <summary>One icon theme file as it is written, without the themes it extends: its name, the theme it
/// extends, its fonts by id and its icons by key, its fonts and pictures read as it is. Read as
/// <see cref="ThemeJson"/> reads, leaving out a bad key, value, font or picture.</summary>
public sealed class IconThemeFile : IThemeFile<IconThemeFile>
{
    public const string Extension = ".json";

    private const string FontsKey = "fonts";
    private const string IconsKey = "icons";
    private const string FontKey = "font";
    private const string GlyphKey = "glyph";
    private const string SvgKey = "svg";
    private const int MaxCodepointDigits = 6;

    private static readonly HashSet<string> Keys = ListKeys();

    /// <summary>Icons the app once drew and no longer does. A theme written while it had them still
    /// names them, so they are passed over without an error.</summary>
    private static readonly HashSet<string> RetiredKeys = ["file.exit"];
    private static readonly byte[] Utf8Bom = [0xEF, 0xBB, 0xBF];

    private readonly ThemeJson _json;
    private readonly IconAssets _assets;
    private readonly Dictionary<string, IconFont> _fonts = new();
    private readonly Dictionary<string, IconValue> _icons = new();

    private IconThemeFile(ThemeId<IconThemeFile> id, string file, IconAssets assets)
    {
        Id = id;
        _json = new ThemeJson(file);
        _assets = assets;
        Name = ThemeName.Plain(id.Text);
    }

    public ThemeId<IconThemeFile> Id { get; }

    public string File => _json.File;

    public ThemeName Name { get; private set; }

    public ThemeId<IconThemeFile>? Extends { get; private set; }

    public int ExtendsLine { get; private set; }

    /// <summary>The fonts the icons can be drawn in, by id.</summary>
    public IReadOnlyDictionary<string, IconFont> Fonts => _fonts;

    /// <summary>The icons, by key.</summary>
    public IReadOnlyDictionary<string, IconValue> Icons => _icons;

    public IReadOnlyList<ThemeError> Errors => _json.Errors;

    public bool IsUnreadable => _json.IsUnreadable;

    /// <summary>Reads the icon theme <paramref name="id"/> from the file at <paramref name="path"/> and the
    /// fonts and pictures it names inside its folder; a file that cannot be read is <see cref="IsUnreadable"/>.</summary>
    public static IconThemeFile ReadFile(ThemeId<IconThemeFile> id, string path)
    {
        var folder = Path.GetDirectoryName(Path.GetFullPath(path))!;
        var theme = new IconThemeFile(id, path, new IconAssetFolder(folder));
        var json = theme._json.ReadBytes(path);
        if (json != null)
            theme._json.Read(json, theme.ReadTopLevel);
        return theme;
    }

    /// <summary>Reads the icon theme <paramref name="id"/> from <paramref name="json"/>, the content of
    /// <paramref name="file"/>, its fonts and pictures from <paramref name="assets"/>.</summary>
    public static IconThemeFile Read(ThemeId<IconThemeFile> id, string file, byte[] json, IconAssets assets)
    {
        var theme = new IconThemeFile(id, file, assets);
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
            case ThemeJson.ExtendsKey:
                ReadExtends(ref reader, line);
                break;
            case FontsKey:
                ReadFonts(ref reader, line);
                break;
            case IconsKey:
                ReadIcons(ref reader, line);
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

    private void ReadExtends(ref Utf8JsonReader reader, int line)
    {
        var parent = _json.ReadString(ref reader, ThemeJson.ExtendsKey, line);
        if (parent == null)
            return;
        var id = ThemeId<IconThemeFile>.Parse(parent);
        if (id == null)
        {
            _json.AddError(line, ThemeProblem.UnknownParent, parent);
            return;
        }
        Extends = id;
        ExtendsLine = line;
    }

    private void ReadFonts(ref Utf8JsonReader reader, int line)
    {
        if (!_json.IsObject(ref reader, FontsKey, line))
            return;
        while (_json.ReadKey(ref reader, out var id, out var idLine))
        {
            var key = FontsKey + "." + id;
            var path = _json.ReadString(ref reader, key, idLine);
            if (path == null)
                continue;
            _fonts[id] = ReadFont(key, id, path, idLine);
        }
    }

    /// <summary>The font <paramref name="id"/> in the file <paramref name="path"/>, loaded once to check
    /// that it can be and to read its characters; broken after reporting why it cannot be.</summary>
    private IconFont ReadFont(string key, string id, string path, int line)
    {
        var bytes = ReadAsset(key, path, line);
        if (bytes == null)
            return new IconFont.Broken(path);
        var face = new FontFace(_assets.FamilyOf(id, path), bytes);
        var characters = face.ReadCharacters();
        if (characters == null)
        {
            _json.AddError(line, ThemeProblem.NotAFont, key, path);
            return new IconFont.Broken(path);
        }
        return new IconFont.Loaded(path, face, characters);
    }

    private void ReadIcons(ref Utf8JsonReader reader, int line)
    {
        if (!_json.IsObject(ref reader, IconsKey, line))
            return;
        while (_json.ReadKey(ref reader, out var key, out var keyLine))
            ReadIcon(ref reader, key, keyLine);
    }

    private void ReadIcon(ref Utf8JsonReader reader, string key, int line)
    {
        var subject = IconsKey + "." + key;
        if (RetiredKeys.Contains(key))
        {
            reader.Skip();
            return;
        }
        if (!Keys.Contains(key))
        {
            _json.SkipUnknown(ref reader, subject, line);
            return;
        }
        if (!_json.IsObject(ref reader, subject, line))
            return;
        var written = ReadIconFields(ref reader, subject);
        var value = MakeValue(subject, written, line);
        if (value != null)
            _icons[key] = value;
    }

    /// <summary>The fields of an icon as written, each null when left out.</summary>
    private readonly record struct IconFields(string? Font, string? Glyph, string? Svg);

    private IconFields ReadIconFields(ref Utf8JsonReader reader, string subject)
    {
        var fields = new IconFields();
        while (_json.ReadKey(ref reader, out var name, out var nameLine))
        {
            var key = subject + "." + name;
            switch (name)
            {
                case FontKey:
                    fields = fields with { Font = _json.ReadString(ref reader, key, nameLine) };
                    break;
                case GlyphKey:
                    fields = fields with { Glyph = _json.ReadString(ref reader, key, nameLine) };
                    break;
                case SvgKey:
                    fields = fields with { Svg = _json.ReadString(ref reader, key, nameLine) };
                    break;
                default:
                    _json.SkipUnknown(ref reader, key, nameLine);
                    break;
            }
        }
        return fields;
    }

    /// <summary>The icon <paramref name="fields"/> make; null after reporting why they make none.</summary>
    private IconValue? MakeValue(string subject, IconFields fields, int line)
    {
        var (font, glyph, svg) = fields;
        if (svg != null && font == null && glyph == null)
            return ReadPicture(subject, svg, line);
        if (svg == null && font != null && glyph != null)
            return MakeGlyph(subject, font, glyph, line);
        _json.AddError(line, ThemeProblem.InvalidIcon, subject);
        return null;
    }

    private IconValue? MakeGlyph(string subject, string font, string glyph, int line)
    {
        var codepoint = ParseCodepoint(glyph);
        if (codepoint == null)
        {
            _json.AddError(line, ThemeProblem.NotACodepoint, subject, glyph);
            return null;
        }
        return new IconValue.Glyph(font, codepoint.Value, File, line);
    }

    /// <summary>The Unicode scalar value <paramref name="hex"/> writes in up to six hex digits, or null.</summary>
    private static int? ParseCodepoint(string hex)
    {
        if (hex.Length == 0 || hex.Length > MaxCodepointDigits)
            return null;
        if (!int.TryParse(hex, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var codepoint))
            return null;
        if (codepoint == 0)
            return null;
        if (!Rune.IsValid(codepoint))
            return null;
        return codepoint;
    }

    private IconValue? ReadPicture(string subject, string path, int line)
    {
        var bytes = ReadAsset(subject, path, line);
        if (bytes == null)
            return null;
        var text = DecodeText(bytes);
        if (!SvgDocument.TryParse(text, out var svg, out var error))
        {
            _json.AddError(line, ThemeProblem.InvalidSvg, subject, $"{path}: {error}");
            return null;
        }
        return new IconValue.Picture(path, svg, File, line);
    }

    private static string DecodeText(byte[] bytes)
    {
        var content = bytes.AsSpan();
        if (content.StartsWith(Utf8Bom))
            content = content.Slice(Utf8Bom.Length);
        return Encoding.UTF8.GetString(content);
    }

    /// <summary>The bytes of the file <paramref name="path"/>; null after reporting why it cannot be read.</summary>
    private byte[]? ReadAsset(string subject, string path, int line)
    {
        var asset = _assets.Read(path);
        if (asset.Bytes == null)
            _json.AddError(line, asset.Problem, subject, asset.Detail);
        return asset.Bytes;
    }

    private static HashSet<string> ListKeys()
    {
        return AppIcons.Keys
            .Select(static k => k.Key)
            .ToHashSet();
    }
}
