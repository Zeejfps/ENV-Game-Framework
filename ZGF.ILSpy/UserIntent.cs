namespace ZGF.ILSpy;

/// <summary>What the user last asked for. Each request takes a ticket; background work started for
/// an older ticket is stale once the user asks for something else.</summary>
public sealed class UserIntent
{
    private const int NoTicket = -1;

    private int _current;
    private int _resolving = NoTicket;

    /// <summary>True while the current request is finding a place in the background.</summary>
    public bool IsResolving => _resolving == _current;

    public int Begin()
    {
        _current++;
        return _current;
    }

    public bool IsCurrent(int ticket) => ticket == _current;

    public bool IsStale(int ticket) => ticket != _current;

    public void StartResolving(int ticket)
    {
        _resolving = ticket;
    }

    public void StopResolving()
    {
        _resolving = NoTicket;
    }
}
