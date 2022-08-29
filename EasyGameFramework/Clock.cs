using System.Diagnostics;
using EasyGameFramework.API;

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
        var deltaTimeTicks = m_Stopwatch.ElapsedTicks;
        DeltaTime = (float)deltaTimeTicks / Stopwatch.Frequency;
        Time += DeltaTime;
        m_Stopwatch.Restart();
    }
}