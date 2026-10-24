namespace ZGF.ILSpy;

/// <summary>Whether writing the settings is failing. A failure is reported once until a write
/// succeeds, and the autosave waits <see cref="DelaySeconds"/> before trying again, from the first
/// frame that asks after the failure.</summary>
public sealed class SaveRetry
{
    public const double DelaySeconds = 2;

    private bool _isFailing;
    private double? _failedAt;

    /// <summary>Notes a failed write; true when writes were not failing before, so it should be reported.</summary>
    public bool Fail()
    {
        var isNew = !_isFailing;
        _isFailing = true;
        _failedAt = null;
        return isNew;
    }

    public void Succeed()
    {
        _isFailing = false;
        _failedAt = null;
    }

    /// <summary>The seconds left at <paramref name="now"/> before a failed write is tried again, or 0.</summary>
    public double Remaining(double now)
    {
        if (!_isFailing)
            return 0;
        if (_failedAt == null)
            _failedAt = now;
        var waited = now - _failedAt.Value;
        return Math.Max(0, DelaySeconds - waited);
    }
}
