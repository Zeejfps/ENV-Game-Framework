namespace ZGF.Desktop;

internal static class IdleWait
{
    /// <summary>How many seconds an idle loop waits for events, or null when <paramref name="earliestWake"/> is
    /// already due and it must not wait. Rounded up to whole milliseconds, at least one, since Win32 truncates
    /// the timeout to milliseconds and a zero wait would spin; never more than <paramref name="maxSeconds"/>.</summary>
    public static double? Choose(StopwatchTimestamp? earliestWake, StopwatchTimestamp now, double maxSeconds)
    {
        if (earliestWake == null)
            return maxSeconds;
        var remaining = earliestWake.Value.SecondsSince(now);
        if (remaining <= 0)
            return null;
        var milliseconds = Math.Max(1, Math.Ceiling(remaining * 1000));
        return Math.Min(milliseconds / 1000, maxSeconds);
    }
}
