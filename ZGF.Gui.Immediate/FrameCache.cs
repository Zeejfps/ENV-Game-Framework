namespace ZGF.Gui.Immediate;

/// <summary>Values kept while asked for this frame or the last, so an unchanged screen never
/// recomputes them and one no longer asked for ages out a frame later.</summary>
internal sealed class FrameCache<TKey, TValue> where TKey : notnull
{
    private Dictionary<TKey, TValue> _current = new();
    private Dictionary<TKey, TValue> _previous = new();

    /// <summary>The value kept for <paramref name="key"/>, which stays kept another frame.</summary>
    public bool TryGet(TKey key, out TValue value)
    {
        if (_current.TryGetValue(key, out value!))
            return true;
        if (!_previous.TryGetValue(key, out value!))
            return false;
        _current[key] = value;
        return true;
    }

    public TValue Keep(TKey key, TValue value)
    {
        _current[key] = value;
        return value;
    }

    /// <summary>Drops what was not asked for last frame, or with <paramref name="invalidate"/> everything.</summary>
    public void BeginFrame(bool invalidate)
    {
        if (invalidate)
            _current.Clear();
        (_current, _previous) = (_previous, _current);
        _current.Clear();
    }
}
