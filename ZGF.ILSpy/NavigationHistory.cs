using ZGF.ILSpy.Core;

namespace ZGF.ILSpy;

/// <summary>Something navigated to: a node's place, or a diagram.</summary>
public abstract record HistoryEntry
{
    private HistoryEntry()
    {
    }

    /// <summary>A place navigated to, how visible its node was, the language it was shown in, and where
    /// its view stood when it was left.</summary>
    public sealed record Node(NodePath Place, NodeAccessibility Accessibility, DecompilationLanguage Language, CodeViewPosition? Position)
        : HistoryEntry;

    /// <summary>A diagram worked in, under the title of its tab.</summary>
    public sealed record Diagram(TypeDiagram Shown, string Title) : HistoryEntry;
}

/// <summary>Back/forward over the places and diagrams navigated to. Places are kept as paths so they
/// outlive reloads and pin no assembly; a diagram is forgotten once its tab closes.</summary>
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
        if (IsCurrentPlace(place))
            return;
        Add(new HistoryEntry.Node(place, node.Accessibility, language, null));
    }

    /// <summary>Whether the place being shown is <paramref name="place"/>.</summary>
    public bool IsCurrentPlace(NodePath? place)
    {
        if (Current is not HistoryEntry.Node node)
            return false;
        return node.Place.Equals(place);
    }

    /// <summary>Records <paramref name="diagram"/>, titled <paramref name="title"/>, as worked in now,
    /// dropping what was forward of the current entry.</summary>
    public void PushDiagram(TypeDiagram diagram, string title)
    {
        if (Current is HistoryEntry.Diagram current && current.Shown == diagram)
            return;
        Add(new HistoryEntry.Diagram(diagram, title));
    }

    private void Add(HistoryEntry entry)
    {
        _entries.RemoveRange(_current + 1, _entries.Count - _current - 1);
        _entries.Add(entry);
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

    /// <summary>Forgets <paramref name="diagram"/>, closed for good; the entry before a forgotten current
    /// one becomes current.</summary>
    public void Forget(TypeDiagram diagram)
    {
        for (var i = _entries.Count - 1; i >= 0; i--)
        {
            if (_entries[i] is not HistoryEntry.Diagram entry)
                continue;
            if (entry.Shown != diagram)
                continue;
            _entries.RemoveAt(i);
            if (i <= _current)
                _current--;
            Revision++;
        }
        if (_current < 0 && _entries.Count > 0)
            _current = 0;
    }

    /// <summary>Remembers where the view of the current place stands as it is left.</summary>
    public void Leave(DecompilationLanguage language, CodeViewPosition position)
    {
        if (Current is not HistoryEntry.Node node)
            return;
        _entries[_current] = node with { Language = language, Position = position };
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
