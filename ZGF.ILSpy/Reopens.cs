namespace ZGF.ILSpy;

/// <summary>The reopen of each listed file that counts: starting another for the file makes the one
/// before it stale, and its result is then ignored.</summary>
public sealed class Reopens
{
    private readonly Dictionary<string, int> _current = new(StringComparer.OrdinalIgnoreCase);
    private int _last;

    public bool IsRunning => _current.Count > 0;

    /// <summary>The paths of the files being reopened.</summary>
    public IReadOnlyCollection<string> Files => _current.Keys;

    /// <summary>Starts a reopen of the file at <paramref name="path"/> and returns its ticket.</summary>
    public int Start(string path)
    {
        _last++;
        _current[path] = _last;
        return _last;
    }

    /// <summary>Ends the reopen of the file at <paramref name="path"/> given <paramref name="ticket"/>.
    /// False when another reopen of it started since, and its result should be ignored.</summary>
    public bool Finish(string path, int ticket)
    {
        if (!_current.TryGetValue(path, out var current))
            return false;
        if (current != ticket)
            return false;
        _current.Remove(path);
        return true;
    }
}
