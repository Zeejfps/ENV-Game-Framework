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
    Diagram,
}

/// <summary>An export as the user asked for it: what it is named after, what it writes, and the
/// folder it writes into.</summary>
public sealed record ExportJob(string Name, ExportKind Kind, string Folder);

/// <summary>Which export of the session one is, through waiting, running and finishing; given by the
/// queue as the export enters it, numbered from 1 in the order they entered.</summary>
public sealed record ExportRun
{
    internal ExportRun(int number) => Number = number;

    internal int Number { get; }
}

/// <summary>An export in the queue: <see cref="Run"/> tells it apart from every other export of
/// the session, through waiting, running and finishing.</summary>
public sealed record QueuedExport(ExportRun Run, ExportJob Job);

/// <summary>How an export's turn in the queue goes: it starts, or it was taken out of the queue
/// before its turn and must not run.</summary>
public abstract record ExportTurn
{
    private protected ExportTurn()
    {
    }
}

/// <summary>What an export works with once its turn comes: the token that cancels it and where it
/// reports how far it has got. <see cref="Run"/> tells the queue which export ends.</summary>
public sealed record ExportStart(ExportRun Run, CancellationToken Token, IProgress<ExportProgress> Progress) : ExportTurn;

/// <summary>The export was taken out of the queue before its turn.</summary>
public sealed record ExportWithdrawn : ExportTurn;

/// <summary>The export that runs: how far it has got, and whether it was asked to stop and has not stopped yet.</summary>
public sealed record RunningExport(QueuedExport Export, ExportProgress Progress, bool IsCancelling);

/// <summary>The exports asked for, run one at a time in the order they were asked for. Only the
/// running one can end its turn, and its progress, which arrives on the UI thread after the
/// background reported it, is dropped once that turn has ended. A waiting export taken out of the
/// queue gets a withdrawn turn at once, which ends it without running.</summary>
public sealed class ExportQueue
{
    private readonly List<Turn> _waiting = new();
    private Active? _active;
    private int _lastRun;

    public bool IsRunning => _active != null;

    /// <summary>The running export, or null when none runs.</summary>
    public RunningExport? Running => _active?.Shown;

    /// <summary>How many exports wait for the running one to end.</summary>
    public int WaitingCount => _waiting.Count;

    /// <summary>The waiting exports, next first.</summary>
    public IEnumerable<QueuedExport> Waiting => _waiting.Select(static t => t.Export);

    /// <summary>Queues <paramref name="job"/>, which has one part until it reports otherwise. The task
    /// completes when its turn comes: at once when nothing runs.</summary>
    public Task<ExportTurn> Enter(ExportJob job)
    {
        _lastRun++;
        var started = new TaskCompletionSource<ExportTurn>(TaskCreationOptions.RunContinuationsAsynchronously);
        _waiting.Add(new Turn(new QueuedExport(new ExportRun(_lastRun), job), started));
        if (!IsRunning)
            StartNext();
        return started.Task;
    }

    private void StartNext()
    {
        var turn = _waiting[0];
        _waiting.RemoveAt(0);
        var export = turn.Export;
        var active = new Active(new RunningExport(export, new ExportProgress(export.Job.Name, 1, 1, null), false));
        _active = active;
        turn.Started.SetResult(new ExportStart(export.Run, active.Cancel.Token, ProgressOf(export.Run)));
    }

    private Progress<ExportProgress> ProgressOf(ExportRun run) => new(p => Take(run, p));

    private void Take(ExportRun run, ExportProgress progress)
    {
        var active = FindActive(run);
        if (active == null)
            return;
        var shown = active.Shown;
        var furthest = Furthest(shown.Progress.Fraction, progress.Fraction);
        active.Shown = shown with { Progress = progress with { Fraction = furthest } };
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
        if (_active == null)
            return;
        _active.Shown = _active.Shown with { IsCancelling = true };
        _active.Cancel.Cancel();
    }

    /// <summary>Takes the waiting export <paramref name="run"/> out of the queue.</summary>
    public void Withdraw(ExportRun run)
    {
        var index = _waiting.FindIndex(t => t.Export.Run == run);
        if (index < 0)
            return;
        var turn = _waiting[index];
        _waiting.RemoveAt(index);
        turn.Started.SetResult(new ExportWithdrawn());
    }

    /// <summary>Ends the turn of the export <paramref name="start"/> began, when it is the running
    /// one, and starts the next.</summary>
    public void Leave(ExportStart start)
    {
        var active = FindActive(start.Run);
        if (active == null)
            return;
        active.Cancel.Dispose();
        _active = null;
        if (_waiting.Count == 0)
            return;
        StartNext();
    }

    /// <summary>The running export's state when <paramref name="run"/> is the running one; null otherwise.</summary>
    private Active? FindActive(ExportRun run)
    {
        if (_active == null)
            return null;
        if (_active.Shown.Export.Run != run)
            return null;
        return _active;
    }

    private sealed record Turn(QueuedExport Export, TaskCompletionSource<ExportTurn> Started);

    /// <summary>The running export as shown, with the source of the token that cancels it.</summary>
    private sealed class Active(RunningExport shown)
    {
        public CancellationTokenSource Cancel { get; } = new();

        public RunningExport Shown { get; set; } = shown;
    }
}
