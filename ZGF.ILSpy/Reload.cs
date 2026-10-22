namespace ZGF.ILSpy;

/// <summary>The reload of the assemblies from disk that counts. Starting or stopping one cancels the
/// one before, whose result is then ignored.</summary>
public sealed class Reload
{
    private int _current;
    private CancellationTokenSource? _work;

    public bool IsRunning => _work != null;

    public ReloadTicket Start()
    {
        Stop();
        var work = new CancellationTokenSource();
        _work = work;
        return new ReloadTicket(_current, work);
    }

    public void Stop()
    {
        _current++;
        _work?.Cancel();
        _work = null;
    }

    /// <summary>Ends the reload <paramref name="ticket"/> was given for. False when another reload
    /// started or it was stopped since, and its result should be ignored.</summary>
    public bool Finish(ReloadTicket ticket)
    {
        ticket.Work.Dispose();
        if (ticket.Id != _current)
            return false;
        _work = null;
        return true;
    }
}

/// <summary>Given for one reload by <see cref="Reload.Start"/>.</summary>
public sealed class ReloadTicket
{
    internal ReloadTicket(int id, CancellationTokenSource work)
    {
        Id = id;
        Work = work;
    }

    internal int Id { get; }

    internal CancellationTokenSource Work { get; }

    public CancellationToken Token => Work.Token;
}
