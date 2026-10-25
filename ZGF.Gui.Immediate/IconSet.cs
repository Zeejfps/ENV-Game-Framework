namespace ZGF.Gui.Immediate;

/// <summary>Icons by key, such as an icon theme's. A key the set lacks is drawn as its fallback set
/// draws it.</summary>
public sealed class IconSet
{
    private readonly Dictionary<string, IconGlyph> _icons;
    private readonly IconSet? _fallback;

    public IconSet(IReadOnlyDictionary<string, IconGlyph> icons, IconSet? fallback = null)
    {
        _icons = new Dictionary<string, IconGlyph>(icons);
        _fallback = fallback;
    }

    /// <summary>The icon of <paramref name="key"/>: this set's, else its fallback's, else one that draws nothing.</summary>
    public IconGlyph Find(string key)
    {
        if (_icons.TryGetValue(key, out var icon))
            return icon;
        if (_fallback != null)
            return _fallback.Find(key);
        return default;
    }

    /// <summary>True when this set itself has an icon for <paramref name="key"/>, not counting its fallback.</summary>
    public bool Contains(string key) => _icons.ContainsKey(key);
}
