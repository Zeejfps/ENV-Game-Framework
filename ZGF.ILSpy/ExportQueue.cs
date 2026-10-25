using ZGF.ILSpy.Core;

namespace ZGF.ILSpy;

/// <summary>What an export writes.</summary>
public enum ExportKind
{
    CSharpFile,
    ILFile,
    Project,
    Solution,
    Pdb,
    Pdbs,
    Files,
}

/// <summary>An export as the user asked for it: what it is named after, what it writes, and the
/// folder it writes into.</summary>
public sealed record ExportJob(string Name, ExportKind Kind, string Folder);

/// <summary>An export in the queue: <see cref="Run"/> tells it apart from every other export of
/// the session, through waiting, running and finishing.</summary>
public sealed record QueuedExport(int Run, ExportJob Job);

/// <summary>What an export works with once its turn comes: the token that cancels it and where it
/// reports how far it has got. <see cref="Run"/> tells the queue which export ends. One
/// <see cref="IsWithdrawn"/> was taken out of the queue before its turn and must not run.</summary>
public readonly record struct ExportStart(int Run, CancellationToken Token, IProgress<ExportProgress> Progress, bool IsWithdrawn = false);

/// <summary>The exports asked for, run one at a time in the order they were asked for. Only the
/// running one can end its turn, and its progress, which arrives on the UI thread after the
/// background reported it, is dropped once that turn has ended. A waiting export taken out of the
/// queue gets a withdrawn turn at once, which ends it without running.</summary>
public sealed class ExportQueue
{
    private readonly List<Turn> _waiting = new();
    private CancellationTokenSource? _cancel;
    private int _lastRun;

    public bool IsRunning => _cancel != null;

    /// <summary>The running export, or null when none runs.</summary>
    public QueuedExport? Running { get; private set; }

    /// <summary>Whether the running export was asked to stop and has not stopped yet.</summary>
    public bool IsCancelling { get; private set; }

    /// <summary>How far the running export has got, or null when none runs.</summary>
    public ExportProgress? Progress { get; private set; }

    /// <summary>How many exports wait for the running one to end.</summary>
    public int WaitingCount => _waiting.Count;

    /// <summary>The waiting exports, next first.</summary>
    public IEnumerable<QueuedExport> Waiting => _waiting.Select(static t => t.Export);

    /// <summary>Queues <paramref name="job"/>, which has one part until it reports otherwise. The task
    /// completes when its turn comes: at once when nothing runs.</summary>
    public Task<ExportStart> Enter(ExportJob job)
    {
        _lastRun++;
        var started = new TaskCompletionSource<ExportStart>(TaskCreationOptions.RunContinuationsAsynchronously);
        _waiting.Add(new Turn(new QueuedExport(_lastRun, job), started));
        if (!IsRunning)
            StartNext();
        return started.Task;
    }

    private void StartNext()
    {
        var turn = _waiting[0];
        _waiting.RemoveAt(0);
        var export = turn.Export;
        Running = export;
        IsCancelling = false;
        _cancel = new CancellationTokenSource();
        Progress = new ExportProgress(export.Job.Name, 1, 1, null);
        var start = new ExportStart(export.Run, _cancel.Token, ProgressOf(export.Run));
        turn.Started.SetResult(start);
    }

    private Progress<ExportProgress> ProgressOf(int run) => new(p => Take(run, p));

    private void Take(int run, ExportProgress progress)
    {
        if (run != Running?.Run)
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

    /// <summary>Asks the running export to stop; the waiting ones still get their turn.</summary>
    public void Cancel()
    {
        if (_cancel == null)
            return;
        IsCancelling = true;
        _cancel.Cancel();
    }

    /// <summary>Takes the waiting export <paramref name="run"/> out of the queue.</summary>
    public void Withdraw(int run)
    {
        var index = _waiting.FindIndex(t => t.Export.Run == run);
        if (index < 0)
            return;
        var turn = _waiting[index];
        _waiting.RemoveAt(index);
        var start = new ExportStart(run, new CancellationToken(true), ProgressOf(run), IsWithdrawn: true);
        turn.Started.SetResult(start);
    }

    /// <summary>Ends the turn of the export <paramref name="start"/> began, when it is the running
    /// one, and starts the next.</summary>
    public void Leave(ExportStart start)
    {
        if (start.Run != Running?.Run)
            return;
        _cancel!.Dispose();
        _cancel = null;
        Running = null;
        IsCancelling = false;
        Progress = null;
        if (_waiting.Count == 0)
            return;
        StartNext();
    }

    private sealed record Turn(QueuedExport Export, TaskCompletionSource<ExportStart> Started);
}
