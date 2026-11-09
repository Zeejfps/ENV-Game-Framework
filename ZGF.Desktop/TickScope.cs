namespace ZGF.Desktop;

/// <summary>One turn of the run loop, handed to each <see cref="IWindowedApp.OnTick"/> handler. Use it only
/// inside the handler, on the loop's thread.</summary>
public sealed class TickScope
{
    internal TickScope() { }

    /// <summary>When this turn's tick began.</summary>
    public StopwatchTimestamp Now { get; private set; }

    internal StopwatchTimestamp? EarliestWake { get; private set; }

    /// <summary>Has the loop, if it goes idle this turn, wake no later than <paramref name="time"/>. Lasts
    /// this turn only. A handler asking for a time already past keeps the loop from waiting at all, so once its
    /// deadline is due it must act on it and stop asking.</summary>
    public void WakeAt(StopwatchTimestamp time)
    {
        if (EarliestWake == null || time < EarliestWake.Value)
            EarliestWake = time;
    }

    internal void Begin(StopwatchTimestamp now)
    {
        Now = now;
        EarliestWake = null;
    }
}
