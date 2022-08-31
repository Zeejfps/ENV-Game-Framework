using System.Diagnostics;

namespace EasyGameFramework.Api;

public abstract class Game : IApp
{
    public bool IsRunning { get; private set; }
    
    private readonly Stopwatch m_Stopwatch;

    protected Game()
    {
        m_Stopwatch = new Stopwatch();
    }
    
    public void Start()
    {
        IsRunning = true;
        OnStart();
    }

    public void Update()
    {
        var deltaTimeTicks = m_Stopwatch.ElapsedTicks;
        var deltaTime = (float)deltaTimeTicks / Stopwatch.Frequency;
        m_Stopwatch.Restart();
        
        OnUpdate(deltaTime);
        OnRender(0);
    }

    public void Quit()
    {
        IsRunning = false;
        OnQuit();
    }

    protected abstract void OnStart();
    protected abstract void OnUpdate(float dt);
    protected abstract void OnRender(float dt);
    protected abstract void OnQuit();
}