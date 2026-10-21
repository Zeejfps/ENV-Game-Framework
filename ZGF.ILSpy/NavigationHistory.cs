using ZGF.ILSpy.Core;

namespace ZGF.ILSpy;

/// <summary>Back/forward over the places navigated to, kept as paths so they outlive reloads and
/// pin no assembly.</summary>
public sealed class NavigationHistory
{
    private const int Capacity = 200;

    private readonly List<NodePath> _entries = new();
    private int _current = -1;

    public bool CanGoBack => _current > 0;
    public bool CanGoForward => _current < _entries.Count - 1;

    /// <summary>Records a new place, dropping what was forward of the current one.</summary>
    public void Push(NodePath place)
    {
        if (_current >= 0 && _entries[_current].Equals(place)) return;
        _entries.RemoveRange(_current + 1, _entries.Count - _current - 1);
        _entries.Add(place);
        if (_entries.Count > Capacity)
            _entries.RemoveAt(0);
        _current = _entries.Count - 1;
    }

    public NodePath? Back() => CanGoBack ? _entries[--_current] : null;

    public NodePath? Forward() => CanGoForward ? _entries[++_current] : null;
}
