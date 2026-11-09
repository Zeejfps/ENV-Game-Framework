using ZGF.Desktop;

namespace ZGF.Gui.Tests;

public sealed class IdleWaitTests
{
    private const double Max = 0.1;
    private static readonly StopwatchTimestamp Now = StopwatchTimestamp.Now();

    [Fact]
    public void NoWakeWaitsTheHousekeepingBound()
    {
        Assert.Equal(Max, IdleWait.Choose(null, Now, Max));
    }

    [Fact]
    public void WaitsUntilTheWake()
    {
        Assert.Equal(0.033, IdleWait.Choose(Now.After(0.033), Now, Max)!.Value, 6);
    }

    [Fact]
    public void AWakeBeyondTheBoundWaitsTheBound()
    {
        Assert.Equal(Max, IdleWait.Choose(Now.After(5), Now, Max));
        Assert.Equal(Max, IdleWait.Choose(Now.After(double.MaxValue), Now, Max));
    }

    [Fact]
    public void ADueWakeSkipsTheWait()
    {
        Assert.Null(IdleWait.Choose(Now, Now, Max));
        Assert.Null(IdleWait.Choose(Now, Now.After(0.01), Max));
    }

    [Fact]
    public void LessThanAMillisecondWaitsOne()
    {
        Assert.Equal(0.001, IdleWait.Choose(Now.After(0.0000001), Now, Max));
        Assert.Equal(0.001, IdleWait.Choose(Now.After(0.0006), Now, Max));
    }

    [Fact]
    public void RoundsUpToWholeMilliseconds()
    {
        Assert.Equal(0.002, IdleWait.Choose(Now.After(0.0012), Now, Max)!.Value, 9);
    }

    [Fact]
    public void TheEarliestWakeOfATurnWins()
    {
        var tick = new TickScope();
        tick.Begin(Now);
        tick.WakeAt(Now.After(0.05));
        tick.WakeAt(Now.After(0.02));
        tick.WakeAt(Now.After(0.08));

        Assert.Equal(Now.After(0.02), tick.EarliestWake);
    }

    [Fact]
    public void ATurnForgetsTheWakesOfTheLast()
    {
        var tick = new TickScope();
        tick.Begin(Now);
        tick.WakeAt(Now.After(0.02));

        tick.Begin(Now.After(0.01));

        Assert.Null(tick.EarliestWake);
        Assert.Equal(Now.After(0.01), tick.Now);
    }
}

public sealed class StopwatchTimestampTests
{
    private static readonly StopwatchTimestamp Now = StopwatchTimestamp.Now();

    [Fact]
    public void SecondsSinceMeasuresTheGapEitherWay()
    {
        var later = Now.After(1.5);

        Assert.Equal(1.5, later.SecondsSince(Now), 6);
        Assert.Equal(-1.5, Now.SecondsSince(later), 6);
    }

    [Fact]
    public void AfterZeroIsTheSameTime()
    {
        Assert.Equal(Now, Now.After(0));
    }

    [Fact]
    public void AfterSaturatesAtTheLatestTime()
    {
        var latest = Now.After(double.MaxValue);

        Assert.Equal(latest, Now.After(double.PositiveInfinity));
        Assert.Equal(latest, latest.After(1));
        Assert.True(Now.After(1e9) < latest);
    }

    [Fact]
    public void AfterRejectsNegativeAndNaN()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Now.After(-0.001));
        Assert.Throws<ArgumentOutOfRangeException>(() => Now.After(double.NaN));
    }

    [Fact]
    public void LaterTimesCompareGreater()
    {
        var later = Now.After(0.001);

        Assert.True(Now < later);
        Assert.True(later > Now);
        Assert.True(Now <= Now);
        Assert.True(later >= Now);
        Assert.True(Now.CompareTo(later) < 0);
    }
}
