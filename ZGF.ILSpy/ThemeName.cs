using System.Globalization;

namespace ZGF.ILSpy;

/// <summary>A theme's name as its file gives it: one name for every language, or a name per language
/// tag (<c>en</c>, <c>pt-BR</c>), where a language without one takes the English name, else the first.</summary>
public sealed class ThemeName
{
    private const string FallbackTag = "en";

    private readonly string _plain;
    private readonly Dictionary<string, string>? _byTag;

    private ThemeName(string plain, Dictionary<string, string>? byTag)
    {
        _plain = plain;
        _byTag = byTag;
    }

    public static ThemeName Plain(string name) => new(name, null);

    /// <summary>A name per language tag, tags compared ignoring case, the first name given for a tag
    /// kept; <paramref name="fallback"/> when there is none at all.</summary>
    public static ThemeName PerLanguage(IReadOnlyDictionary<string, string> byTag, string fallback)
    {
        var tags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (tag, name) in byTag)
            tags.TryAdd(tag, name);
        return new ThemeName(fallback, tags);
    }

    /// <summary>The name to show in <paramref name="culture"/>'s language.</summary>
    public string In(CultureInfo culture)
    {
        if (_byTag == null)
            return _plain;
        if (_byTag.TryGetValue(culture.Name, out var name))
            return name;
        if (_byTag.TryGetValue(FallbackTag, out var english))
            return english;
        return FirstOrPlain(_byTag);
    }

    private string FirstOrPlain(Dictionary<string, string> byTag)
    {
        foreach (var name in byTag.Values)
            return name;
        return _plain;
    }
}
