namespace ZGF.ILSpy;

/// <summary>The files being opened in the background. Cancelling them all, as clearing the list does,
/// keeps what they open out of the list that comes next.</summary>
public sealed class OpenRequests
{
    private CancellationTokenSource _work = new();

    /// <summary>The token of the opens starting now, cancelled by the next <see cref="CancelAll"/>.</summary>
    public CancellationToken Token => _work.Token;

    public void CancelAll()
    {
        _work.Cancel();
        _work = new CancellationTokenSource();
    }
}
