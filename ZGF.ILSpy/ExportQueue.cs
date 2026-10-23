using ZGF.ILSpy.Core;

namespace ZGF.ILSpy;

/// <summary>What an export works with once its turn comes: the token that cancels it and where it
/// reports how far it has got. <see cref="Run"/> tells the queue which export ends.</summary>
public readonly record struct ExportStart(int Run, CancellationToken Token, IProgress<ExportProgress> Progress);

/// <summary>An export waiting for its turn.</summary>
public sealed record QueuedExport(int Run, string Name);

/// <summary>The exports asked for, run one at a time in the order they were asked for. Only the
/// running one can end its turn, and its progress, which arrives on the UI thread after the
/// background reported it, is dropped once that turn has ended. A waiting export taken out of the
/// queue gets its turn at once, already cancelled, so it ends as a cancelled export does.</summary>
public sealed class ExportQueue
{
    private static readonly CancellationToken Withdrawn = new(true);

    private readonly List<Turn> _waiting = new();
    private CancellationTokenSource? _cancel;
    private int _lastRun;
    private int _running;

    public bool IsRunning => _cancel != null;

    /// <summary>The name the running export was queued under, or null when none runs.</summary>
    public string? RunningName { get; private set; }

    /// <summary>How far the running export has got, or null when none runs.</summary>
    public ExportProgress? Progress { get; private set; }

    /// <summary>How many exports wait for the running one to end.</summary>
    public int WaitingCount => _waiting.Count;

    /// <summary>The waiting exports, next first.</summary>
    public IEnumerable<QueuedExport> Waiting => _waiting.Select(static t => t.Export);

    /// <summary>Queues the export named <paramref name="name"/>, which has one part until it reports
    /// otherwise. The task completes when its turn comes: at once when nothing runs.</summary>
    public Task<ExportStart> Enter(string name)
    {
        _lastRun++;
        var started = new TaskCompletionSource<ExportStart>(TaskCreationOptions.RunContinuationsAsynchronously);
        _waiting.Add(new Turn(new QueuedExport(_lastRun, name), started));
        if (!IsRunning)
            StartNext();
        return started.Task;
    }

    private void StartNext()
    {
        var turn = _waiting[0];
        _waiting.RemoveAt(0);
        var run = turn.Export.Run;
        _running = run;
        _cancel = new CancellationTokenSource();
        RunningName = turn.Export.Name;
        Progress = new ExportProgress(turn.Export.Name, 1, 1, null);
        var start = new ExportStart(run, _cancel.Token, ProgressOf(run));
        turn.Started.SetResult(start);
    }

    private Progress<ExportProgress> ProgressOf(int run) => new(p => Take(run, p));

    private void Take(int run, ExportProgress progress)
    {
        if (run != _running)
            return;
        var furthest = Furthest(Progress?.Fraction, progress.Fraction);
        Progress = progress with { Fraction = furthest };
    }

    /// <summary>The further of two fractions of one export: reports sent from several threads can
    /// arrive out of order, and the bar must not step back.</summary>
    private static float? Furthest(float? shown, float? reported)
    {
        if (shown == null)
            return reported;
        if (reported == null)
            return shown;
        return Math.Max(shown.Value, reported.Value);
    }

    /// <summary>Cancels the running export; the waiting ones still get their turn.</summary>
    public void Cancel() => _cancel?.Cancel();

    /// <summary>Takes the waiting export <paramref name="run"/> out of the queue.</summary>
    public void Withdraw(int run)
    {
        var index = _waiting.FindIndex(t => t.Export.Run == run);
        if (index < 0)
            return;
        var turn = _waiting[index];
        _waiting.RemoveAt(index);
        var start = new ExportStart(run, Withdrawn, ProgressOf(run));
        turn.Started.SetResult(start);
    }

    /// <summary>Ends the turn of the export <paramref name="start"/> began, when it is the running
    /// one, and starts the next.</summary>
    public void Leave(ExportStart start)
    {
        if (start.Run != _running)
            return;
        _cancel!.Dispose();
        _cancel = null;
        _running = 0;
        RunningName = null;
        Progress = null;
        if (_waiting.Count == 0)
            return;
        StartNext();
    }

    private sealed record Turn(QueuedExport Export, TaskCompletionSource<ExportStart> Started);
}
