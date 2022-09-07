using System.Diagnostics;

namespace EasyGameFramework.Api;

public class Clock : IClock
{
    public event Action? Ticked;
    
    public float DeltaTime { get; private set; }
    public float Time { get; private set; }
    public float TimeScale { get; set; } = 1f;

    private IEventLoop EventLoop { get; }
    private Stopwatch Stopwatch { get; }

    public Clock(IEventLoop eventLoop)
    {
        EventLoop = eventLoop;
        Stopwatch = new Stopwatch();
    }

    public void Start()
    {
        Time = 0f;
        Stopwatch.Start();
        EventLoop.OnEarlyUpdate += Tick;
    }

    public void Stop()
    {
        Stopwatch.Stop();
        EventLoop.OnEarlyUpdate -= Tick;
    }

    private void Tick()
    {
        var deltaTimeTicks = Stopwatch.ElapsedTicks;
        Stopwatch.Restart();
        DeltaTime = (float)deltaTimeTicks / Stopwatch.Frequency * TimeScale;
        Time += DeltaTime;
        Ticked?.Invoke();
    }
}