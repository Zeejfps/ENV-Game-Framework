using ZGF.ILSpy.Core;

namespace ZGF.ILSpy;

/// <summary>A place navigated to, how visible its node was, the language it was shown in, and
/// where its view stood when it was left.</summary>
public readonly record struct HistoryEntry(NodePath Place, NodeAccessibility Accessibility, DecompilationLanguage Language, CodeViewPosition? Position);

/// <summary>Back/forward over the places navigated to, kept as paths so they outlive reloads and
/// pin no assembly.</summary>
public sealed class NavigationHistory
{
    private const int Capacity = 200;

    private readonly List<HistoryEntry> _entries = new();
    private int _current = -1;

    /// <summary>Every place recorded, oldest first.</summary>
    public IReadOnlyList<HistoryEntry> Entries => _entries;

    /// <summary>The index in <see cref="Entries"/> of the place being shown, or -1 before any.</summary>
    public int CurrentIndex => _current;

    /// <summary>Changes whenever an entry is added or dropped, or another becomes current.</summary>
    public int Revision { get; private set; }

    public bool CanGoBack => _current > 0;
    public bool CanGoForward => _current < _entries.Count - 1;

    public HistoryEntry? Current => _current >= 0 ? _entries[_current] : null;

    /// <summary>Records a new place, dropping what was forward of the current one.</summary>
    public void Push(BrowseNode node, DecompilationLanguage language)
    {
        var place = node.Path;
        if (_current >= 0 && _entries[_current].Place.Equals(place)) return;
        _entries.RemoveRange(_current + 1, _entries.Count - _current - 1);
        _entries.Add(new HistoryEntry(place, node.Accessibility, language, null));
        if (_entries.Count > Capacity)
            _entries.RemoveAt(0);
        _current = _entries.Count - 1;
        Revision++;
    }

    /// <summary>Forgets every place.</summary>
    public void Clear()
    {
        _entries.Clear();
        _current = -1;
        Revision++;
    }

    /// <summary>Remembers where the view of the current place stands as it is left.</summary>
    public void Leave(DecompilationLanguage language, CodeViewPosition position)
    {
        if (_current >= 0)
            _entries[_current] = _entries[_current] with { Language = language, Position = position };
    }

    /// <summary>Makes the entry at <paramref name="index"/> current, keeping every entry on either side.</summary>
    public HistoryEntry? MoveTo(int index)
    {
        if (index < 0 || index >= _entries.Count || index == _current) return null;
        _current = index;
        Revision++;
        return _entries[index];
    }
}
