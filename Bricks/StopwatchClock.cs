using System.Diagnostics;
using Bricks;

public sealed class StopwatchClock : IClock
{
    public bool IsRunning { get; private set; }
    public float DeltaTimeSeconds { get; private set; }

    private readonly Stopwatch _stopwatch;

    public StopwatchClock()
    {
        _stopwatch = new Stopwatch();
    }

    public void Start()
    {
        IsRunning = true;
        DeltaTimeSeconds = 0;
        _stopwatch.Start();
    }
    
    public void Update()
    {
        if (!IsRunning)
        {
            DeltaTimeSeconds = 0;
            return;
        }
        
        var deltaTimeMs = _stopwatch.ElapsedMilliseconds;
        DeltaTimeSeconds = deltaTimeMs / 1000.0f;
        _stopwatch.Restart();
    }

    public void Stop()
    {
        _stopwatch.Stop();
        DeltaTimeSeconds = 0;
        IsRunning = false;
    }

    public void StepForward()
    {
        DeltaTimeSeconds = 0.05f;
    }
}