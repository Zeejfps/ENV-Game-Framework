using System.Diagnostics;

namespace ZGF.Gui.Immediate;

/// <summary>A readiness step that could not run on the thread owning the frames before the wait's deadline.</summary>
internal class OwnerBusyException(string message) : TimeoutException(message);

/// <summary>What a readiness step found once its frame, if it ran one, had ended.</summary>
internal readonly record struct ReadinessCheck(FramePending Pending, bool Done);

/// <summary>
/// When the frames of one <see cref="Ui"/> are ready, as every harness that drives them decides it: what is still
/// pending once a frame ends, readiness that runs frames at frozen time while a frame asks for the next to finish a
/// change of state or work is posted, and settling that runs animation out. Harnesses differ only in the thread
/// they run frames on and their clock. Everything but <see cref="RequestFrame"/>, <see cref="Stop"/> and the
/// waiting inside <see cref="WaitUntil"/> runs on the thread that owns the frames.
/// </summary>
internal sealed class FrameReadiness
{
    /// <summary>What readiness runs more frames for, without moving the clock.</summary>
    public const FramePending Drained = FramePending.Update | FramePending.FrameRequest;

    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(10);

    private readonly object _gate = new();
    private readonly BackgroundWork? _background;
    private bool _frameRequested;
    private bool _stopped;
    private long _finishedBeforeFrame;
    private double _scheduledAt = double.PositiveInfinity;

    /// <param name="background">The work readiness waits for, which no other harness may be given.</param>
    public FrameReadiness(BackgroundWork? background)
    {
        _background = background;
        background?.Attach(RequestFrame);
    }

    public BackgroundWork? Background => _background;

    /// <summary>Asks for another frame, as posted work does, and wakes a wait. Safe from any thread.</summary>
    public void RequestFrame()
    {
        lock (_gate)
        {
            _frameRequested = true;
            Monitor.PulseAll(_gate);
        }
    }

    /// <summary>Wakes every wait for good, as the frames will run no more. Safe from any thread.</summary>
    public void Stop()
    {
        lock (_gate)
        {
            _stopped = true;
            Monitor.PulseAll(_gate);
        }
    }

    /// <summary>A frame starts: what was requested before it is what it answers, and work finishing from here on
    /// asks for the next.</summary>
    public void BeginFrame()
    {
        lock (_gate)
            _frameRequested = false;
        if (_background != null)
            _finishedBeforeFrame = _background.Progress().Finished;
    }

    /// <summary>The frame's clock reads <paramref name="now"/>: a scheduled update due by then is answered by it.</summary>
    public void AdvanceTo(double now)
    {
        if (now >= _scheduledAt)
            _scheduledAt = double.PositiveInfinity;
    }

    /// <summary>The frame of <paramref name="ui"/> has ended; the earliest non-ambient update it scheduled is pending.</summary>
    public void EndFrame(Ui ui) => _scheduledAt = Math.Min(_scheduledAt, ui.NonAmbientWakeAt);

    /// <summary>What still wants a frame now that <paramref name="ui"/>'s last one has ended. Work that finished since
    /// that frame began counts as a frame request whether or not its wake has arrived yet.</summary>
    public FramePending Pending(Ui ui)
    {
        var pending = FramePending.None;
        if (_background != null)
        {
            var (outstanding, finished) = _background.Progress();
            if (outstanding > 0)
                pending |= FramePending.BackgroundWork;
            if (finished != _finishedBeforeFrame)
                pending |= FramePending.FrameRequest;
        }
        lock (_gate)
        {
            if (_frameRequested)
                pending |= FramePending.FrameRequest;
        }
        if (ui.RequiredUpdateRequested)
            pending |= FramePending.Update;
        if (ui.AnimationFrameRequested)
            pending |= FramePending.Animation;
        if (double.IsFinite(_scheduledAt))
            pending |= FramePending.ScheduledUpdate;
        return pending;
    }

    /// <summary>What still moves over time after the last frame, which readiness does not wait for and settling runs out.</summary>
    public FramePending Animating(Ui ui) => Pending(ui) & (FramePending.Animation | FramePending.ScheduledUpdate);

    /// <summary>Throws what tracked background work failed with, then reports what is pending and whether
    /// <paramref name="done"/> holds.</summary>
    public ReadinessCheck Check(Ui ui, Func<bool> done)
    {
        ThrowBackgroundFailures();
        return new ReadinessCheck(Pending(ui), done());
    }

    /// <summary>What readiness waits out: frames to drain, and tracked background work when <paramref name="untilBackgroundDone"/>.</summary>
    public static FramePending Blocking(bool untilBackgroundDone) =>
        Drained | (untilBackgroundDone ? FramePending.BackgroundWork : FramePending.None);

    /// <summary>Runs frames, each of <paramref name="runFrame"/>, until one ends with nothing but background work
    /// pending, or <paramref name="maxSeconds"/> of the clock <paramref name="now"/> reads have passed.</summary>
    public static SettleResult Settle(Func<FramePending> runFrame, Func<double> now, double maxSeconds)
    {
        var start = now();
        while (true)
        {
            var pending = runFrame() & ~FramePending.BackgroundWork;
            var seconds = now() - start;
            if (pending == FramePending.None || seconds >= maxSeconds)
                return new SettleResult(pending, seconds);
        }
    }

    public static long Deadline(TimeSpan timeout)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(timeout, TimeSpan.Zero);
        var ticks = Math.Min(timeout.TotalSeconds * Stopwatch.Frequency, long.MaxValue / 4.0);
        return Stopwatch.GetTimestamp() + (long)ticks;
    }

    public static TimeSpan Remaining(long deadline) => Stopwatch.GetElapsedTime(Stopwatch.GetTimestamp(), deadline);

    /// <summary>Waits, on the calling thread, until a step finds none of <paramref name="blocking"/> pending and its
    /// condition holding. <paramref name="step"/> runs on the thread owning the frames, running a frame first when
    /// asked to: it is asked to at once, and again without the clock moving while <see cref="Drained"/> is pending;
    /// otherwise this waits for a frame request or a poll. Throws a <see cref="TimeoutException"/> saying what was
    /// still pending once <paramref name="deadline"/> passes, or once a step throws an <see cref="OwnerBusyException"/>.</summary>
    public void WaitUntil(Func<bool, ReadinessCheck> step, FramePending blocking, TimeSpan timeout, long deadline)
    {
        var pending = FramePending.None;
        try
        {
            var state = step(true);
            while (true)
            {
                pending = state.Pending & blocking;
                var runFrame = (pending & Drained) != 0;
                if (!runFrame && pending == FramePending.None && state.Done)
                    return;
                var remaining = Remaining(deadline);
                if (remaining <= TimeSpan.Zero)
                    throw NotReady(pending, timeout, null);
                if (runFrame)
                {
                    state = step(true);
                    continue;
                }
                lock (_gate)
                {
                    if (!_frameRequested && !_stopped)
                        Monitor.Wait(_gate, remaining < PollInterval ? remaining : PollInterval);
                }
                state = step(false);
            }
        }
        catch (OwnerBusyException busy)
        {
            throw NotReady(pending, timeout, busy);
        }
    }

    private void ThrowBackgroundFailures()
    {
        if (_background == null) return;
        var failures = _background.TakeFailures();
        if (failures.Length == 0) return;
        var names = string.Join(", ", failures.Select(f => f.Name));
        throw new AggregateException($"Background work failed: {names}.", failures.Select(f => f.Error));
    }

    private TimeoutException NotReady(FramePending pending, TimeSpan timeout, OwnerBusyException? busy)
    {
        var reasons = new List<string>();
        if (busy != null)
            reasons.Add(busy.Message);
        if ((pending & FramePending.Update) != 0)
            reasons.Add("every frame asked for the next with RequestUpdate, which state that settles stops doing; what moves over time asks with RequestAnimationFrame, which Settle runs out");
        if ((pending & FramePending.FrameRequest) != 0)
            reasons.Add("work kept being posted, or frames requested, after every frame");
        if ((pending & FramePending.BackgroundWork) != 0 && _background != null)
            reasons.Add($"background work still running: {string.Join(", ", _background.Outstanding)}");
        if (reasons.Count == 0)
            reasons.Add("the condition never held");
        return new TimeoutException($"Not ready after {timeout.TotalSeconds:0.###}s: {string.Join("; ", reasons)}.", busy);
    }
}
