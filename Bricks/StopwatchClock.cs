using System.Diagnostics;
using Bricks;

public sealed class StopwatchClock : IClock
{
    public float DeltaTimeSeconds { get; private set; }

    private readonly Stopwatch _stopwatch;

    public StopwatchClock()
    {
        _stopwatch = new Stopwatch();
    }

    public void Start()
    {
        _stopwatch.Start();
    }
    
    public void Update()
    {
        var deltaTimeMs = _stopwatch.ElapsedMilliseconds;
        DeltaTimeSeconds = deltaTimeMs / 1000.0f;
    }

    public void Stop()
    {
        _stopwatch.Stop();
    }
}