using ZGF.ILSpy.Core;

namespace ZGF.ILSpy;

/// <summary>What a started export works with: the token that cancels it and where it reports how
/// far it has got.</summary>
public readonly record struct ExportStart(CancellationToken Token, IProgress<ExportProgress> Progress);

/// <summary>The export running in the background, one at a time. Its progress arrives on the UI
/// thread after the background reported it, so what arrives once the run has ended is dropped.</summary>
public sealed class ExportRun
{
    private CancellationTokenSource? _cancel;
    private int _run;

    public bool IsRunning => _cancel != null;

    /// <summary>How far the running export has got, or null when none runs.</summary>
    public ExportProgress? Progress { get; private set; }

    /// <summary>Starts a run of the export named <paramref name="name"/>, which has one part until it
    /// reports otherwise.</summary>
    public ExportStart Begin(string name)
    {
        _run++;
        _cancel = new CancellationTokenSource();
        Progress = new ExportProgress(name, 1, 1, 0, 0);
        var run = _run;
        var progress = new Progress<ExportProgress>(p => Take(run, p));
        return new ExportStart(_cancel.Token, progress);
    }

    private void Take(int run, ExportProgress progress)
    {
        if (run != _run)
            return;
        if (!IsRunning)
            return;
        Progress = progress;
    }

    public void Cancel() => _cancel?.Cancel();

    public void End()
    {
        _cancel?.Dispose();
        _cancel = null;
        Progress = null;
    }
}
