using System.Diagnostics;

namespace ZGF.Desktop;

/// <summary>A point in time on the <see cref="Stopwatch"/> clock.</summary>
public readonly record struct StopwatchTimestamp : IComparable<StopwatchTimestamp>
{
    private static readonly StopwatchTimestamp Latest = new(long.MaxValue);

    private readonly long _ticks;

    private StopwatchTimestamp(long ticks) => _ticks = ticks;

    public static StopwatchTimestamp Now() => new(Stopwatch.GetTimestamp());

    /// <summary>The time <paramref name="seconds"/> later, or the latest there is when that lies beyond it.</summary>
    public StopwatchTimestamp After(double seconds)
    {
        if (!(seconds >= 0))
            throw new ArgumentOutOfRangeException(nameof(seconds), seconds, "Must be zero or more.");
        var delta = seconds * Stopwatch.Frequency;
        if (delta >= long.MaxValue)
            return Latest;
        var target = (Int128)_ticks + (long)delta;
        if (target >= long.MaxValue)
            return Latest;
        return new StopwatchTimestamp((long)target);
    }

    /// <summary>The seconds from <paramref name="earlier"/> to this time; negative when it is later.</summary>
    public double SecondsSince(StopwatchTimestamp earlier) => (_ticks - earlier._ticks) / (double)Stopwatch.Frequency;

    public int CompareTo(StopwatchTimestamp other) => _ticks.CompareTo(other._ticks);

    public static bool operator <(StopwatchTimestamp a, StopwatchTimestamp b) => a._ticks < b._ticks;
    public static bool operator >(StopwatchTimestamp a, StopwatchTimestamp b) => a._ticks > b._ticks;
    public static bool operator <=(StopwatchTimestamp a, StopwatchTimestamp b) => a._ticks <= b._ticks;
    public static bool operator >=(StopwatchTimestamp a, StopwatchTimestamp b) => a._ticks >= b._ticks;
}
