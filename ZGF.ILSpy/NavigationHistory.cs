using ZGF.ILSpy.Core;

namespace ZGF.ILSpy;

/// <summary>A place navigated to: a node, and the line of its document to show, or -1 for its
/// definition.</summary>
public readonly record struct HistoryEntry(BrowseNode Node, int Line);

/// <summary>Back/forward over the places navigated to.</summary>
public sealed class NavigationHistory
{
    private const int Capacity = 200;

    private readonly List<HistoryEntry> _entries = new();
    private int _current = -1;

    public bool CanGoBack => _current > 0;
    public bool CanGoForward => _current < _entries.Count - 1;

    /// <summary>Records a new place, dropping what was forward of the current one.</summary>
    public void Push(HistoryEntry entry)
    {
        if (_current >= 0 && ReferenceEquals(_entries[_current].Node, entry.Node))
        {
            _entries[_current] = entry;
            return;
        }
        _entries.RemoveRange(_current + 1, _entries.Count - _current - 1);
        _entries.Add(entry);
        if (_entries.Count > Capacity)
            _entries.RemoveAt(0);
        _current = _entries.Count - 1;
    }

    public HistoryEntry? Back() => CanGoBack ? _entries[--_current] : null;

    public HistoryEntry? Forward() => CanGoForward ? _entries[++_current] : null;
}
