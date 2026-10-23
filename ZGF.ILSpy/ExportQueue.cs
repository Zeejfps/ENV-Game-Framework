using ZGF.ILSpy.Core;

namespace ZGF.ILSpy;

/// <summary>What an export works with once its turn comes: the token that cancels it and where it
/// reports how far it has got. <see cref="Run"/> tells the queue which export ends.</summary>
public readonly record struct ExportStart(int Run, CancellationToken Token, IProgress<ExportProgress> Progress);

/// <summary>The exports asked for, run one at a time in the order they were asked for. Only the
/// running one can end its turn, and its progress, which arrives on the UI thread after the
/// background reported it, is dropped once that turn has ended.</summary>
public sealed class ExportQueue
{
    private readonly Queue<Turn> _waiting = new();
    private CancellationTokenSource? _cancel;
    private int _lastRun;
    private int _running;

    public bool IsRunning => _cancel != null;

    /// <summary>How many exports wait for the running one to end.</summary>
    public int WaitingCount => _waiting.Count;

    /// <summary>How far the running export has got, or null when none runs.</summary>
    public ExportProgress? Progress { get; private set; }

    /// <summary>Queues the export named <paramref name="name"/>, which has one part until it reports
    /// otherwise. The task completes when its turn comes: at once when nothing runs.</summary>
    public Task<ExportStart> Enter(string name)
    {
        _lastRun++;
        var started = new TaskCompletionSource<ExportStart>(TaskCreationOptions.RunContinuationsAsynchronously);
        _waiting.Enqueue(new Turn(_lastRun, name, started));
        if (!IsRunning)
            StartNext();
        return started.Task;
    }

    private void StartNext()
    {
        var turn = _waiting.Dequeue();
        _running = turn.Run;
        _cancel = new CancellationTokenSource();
        Progress = new ExportProgress(turn.Name, 1, 1, null);
        var progress = new Progress<ExportProgress>(p => Take(turn.Run, p));
        var start = new ExportStart(turn.Run, _cancel.Token, progress);
        turn.Started.SetResult(start);
    }

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

    /// <summary>Ends the turn of the export <paramref name="start"/> began, when it is the running
    /// one, and starts the next.</summary>
    public void Leave(ExportStart start)
    {
        if (start.Run != _running)
            return;
        _cancel!.Dispose();
        _cancel = null;
        _running = 0;
        Progress = null;
        if (_waiting.Count == 0)
            return;
        StartNext();
    }

    private sealed record Turn(int Run, string Name, TaskCompletionSource<ExportStart> Started);
}
