using System.Diagnostics;

namespace Framework;

public class Clock : IClock
{
    public float DeltaTime { get; private set; }
    public float Time { get; private set; }
    
    private readonly Stopwatch m_Stopwatch;
    
    public Clock()
    {
        m_Stopwatch = new Stopwatch();
    }

    public void Tick()
    {
        var deltaTimeMilli = m_Stopwatch.ElapsedMilliseconds;
        DeltaTime = deltaTimeMilli / 1000f;
        Time += DeltaTime;
        m_Stopwatch.Restart();
    }
}