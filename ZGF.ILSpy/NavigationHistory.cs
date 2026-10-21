using ZGF.ILSpy.Core;

namespace ZGF.ILSpy;

/// <summary>A place navigated to, the language it was shown in, and where its view stood when it
/// was left.</summary>
public readonly record struct HistoryEntry(NodePath Place, DecompilationLanguage Language, CodeViewPosition? Position);

/// <summary>Back/forward over the places navigated to, kept as paths so they outlive reloads and
/// pin no assembly.</summary>
public sealed class NavigationHistory
{
    private const int Capacity = 200;

    private readonly List<HistoryEntry> _entries = new();
    private int _current = -1;

    public bool CanGoBack => _current > 0;
    public bool CanGoForward => _current < _entries.Count - 1;

    public HistoryEntry? Current => _current >= 0 ? _entries[_current] : null;

    /// <summary>Records a new place, dropping what was forward of the current one.</summary>
    public void Push(NodePath place, DecompilationLanguage language)
    {
        if (_current >= 0 && _entries[_current].Place.Equals(place)) return;
        _entries.RemoveRange(_current + 1, _entries.Count - _current - 1);
        _entries.Add(new HistoryEntry(place, language, null));
        if (_entries.Count > Capacity)
            _entries.RemoveAt(0);
        _current = _entries.Count - 1;
    }

    /// <summary>Remembers where the view of the current place stands as it is left.</summary>
    public void Leave(DecompilationLanguage language, CodeViewPosition position)
    {
        if (_current >= 0)
            _entries[_current] = _entries[_current] with { Language = language, Position = position };
    }

    public HistoryEntry? Back() => CanGoBack ? _entries[--_current] : null;

    public HistoryEntry? Forward() => CanGoForward ? _entries[++_current] : null;
}
