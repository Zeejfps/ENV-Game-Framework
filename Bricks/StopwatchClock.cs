using System.Diagnostics;
using Bricks;

public sealed class StopwatchClock : IClock
{
    public bool IsRunning { get; private set; }
    public float DeltaTimeSeconds { get; private set; }

    private readonly Stopwatch _stopwatch;
    private bool _reset;

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
        if (_reset)
        {
            _reset = false;
            DeltaTimeSeconds = 0;
            return;
        }
        
        if (!IsRunning)
            return;
        
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
        _reset = true;
    }
}