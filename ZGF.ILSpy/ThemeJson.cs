using System.Text.Json;

namespace ZGF.ILSpy;

/// <summary>One color or icon theme file's JSON as it is read: comments and trailing commas allowed,
/// every token's line known, and what was wrong collected in <see cref="Errors"/> with the file and line,
/// reading going on past a bad key or value. A file that is no JSON object at all is
/// <see cref="IsUnreadable"/>.</summary>
internal sealed class ThemeJson(string file)
{
    public const string SchemaKey = "$schema";
    public const string NameKey = "name";
    public const string ExtendsKey = "extends";

    private static readonly byte[] Utf8Bom = [0xEF, 0xBB, 0xBF];

    private static readonly JsonReaderOptions Options = new()
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private readonly List<ThemeError> _errors = new();
    private List<int> _lineStarts = new();

    /// <summary>Reads the value of a key at the top of the file, the reader on that value, which it must
    /// move past.</summary>
    public delegate void TopLevelKey(ref Utf8JsonReader reader, string key, int line);

    public string File => file;

    public IReadOnlyList<ThemeError> Errors => _errors;

    /// <summary>True when the file could not be read as a JSON object, so nothing in it counts.</summary>
    public bool IsUnreadable { get; private set; }

    /// <summary>Reads the file at <paramref name="path"/>; null after marking it unreadable.</summary>
    public byte[]? ReadBytes(string path)
    {
        try
        {
            return System.IO.File.ReadAllBytes(path);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            MarkUnreadable(0, ThemeProblem.Unreadable, e.Message);
            return null;
        }
    }

    /// <summary>Reads <paramref name="json"/>, which must hold one object, handing each of its keys to
    /// <paramref name="readKey"/>.</summary>
    public void Read(byte[] json, TopLevelKey readKey)
    {
        var content = SkipBom(json);
        _lineStarts = FindLineStarts(content);
        var reader = new Utf8JsonReader(content, Options);
        try
        {
            ReadRoot(ref reader, readKey);
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

    private void ReadRoot(ref Utf8JsonReader reader, TopLevelKey readKey)
    {
        reader.Read();
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            MarkUnreadable(LineOf(reader), ThemeProblem.RootNotAnObject);
            return;
        }
        while (ReadKey(ref reader, out var key, out var line))
            readKey(ref reader, key, line);
        reader.Read();
    }

    /// <summary>Moves past the next key of the object the reader is in, onto its value. False at the
    /// object's end.</summary>
    public bool ReadKey(ref Utf8JsonReader reader, out string key, out int line)
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

    /// <summary>The name the reader is on: a string, or an object of names by language tag. Null after
    /// reporting that it is neither.</summary>
    public ThemeName? ReadName(ref Utf8JsonReader reader, int line, string fallback)
    {
        if (reader.TokenType == JsonTokenType.String)
            return ThemeName.Plain(reader.GetString()!);
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            AddError(line, ThemeProblem.InvalidName);
            reader.Skip();
            return null;
        }
        var byTag = ReadNamesByTag(ref reader);
        return ThemeName.PerLanguage(byTag, fallback);
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

    /// <summary>True when the reader is on an object; else skips the value after reporting that it is not one.</summary>
    public bool IsObject(ref Utf8JsonReader reader, string key, int line)
    {
        if (reader.TokenType == JsonTokenType.StartObject)
            return true;
        AddError(line, ThemeProblem.NotAnObject, key);
        reader.Skip();
        return false;
    }

    /// <summary>The string the reader is on, or null after reporting that it is something else.</summary>
    public string? ReadString(ref Utf8JsonReader reader, string key, int line)
    {
        if (reader.TokenType == JsonTokenType.String)
            return reader.GetString();
        AddError(line, ThemeProblem.NotAString, key);
        reader.Skip();
        return null;
    }

    /// <summary>Skips the value of <paramref name="key"/> after reporting that no theme file takes it.</summary>
    public void SkipUnknown(ref Utf8JsonReader reader, string key, int line)
    {
        AddError(line, ThemeProblem.UnknownKey, key);
        reader.Skip();
    }

    private int LineOf(Utf8JsonReader reader)
    {
        var offset = (int)reader.TokenStartIndex;
        var index = _lineStarts.BinarySearch(offset);
        if (index >= 0)
            return index + 1;
        return ~index;
    }

    public void MarkUnreadable(int line, ThemeProblem problem, string detail = "")
    {
        IsUnreadable = true;
        AddError(line, problem, "", detail);
    }

    public void AddError(int line, ThemeProblem problem, string subject = "", string detail = "")
    {
        _errors.Add(new ThemeError(File, line, problem, subject, detail));
    }
}
