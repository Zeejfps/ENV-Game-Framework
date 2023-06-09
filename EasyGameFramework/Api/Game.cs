using System.Diagnostics;
using EasyGameFramework.Core;

namespace EasyGameFramework.Api;

public abstract class Game
{
    public event Action? Stopped;
    
    public bool IsRunning { get; private set; }
    
    protected IGameClock Clock => m_Clock;
    protected IWindow Window { get; }
    protected ILogger Logger { get; }

    private float m_DeltaTime = 1f / 60f;
    private double m_Accumulator = 0.0;
    
    private readonly Stopwatch m_Stopwatch;
    private readonly GameClock m_Clock;

    private double m_FpsTime;
    private int m_FrameCount;
    
    protected Game(IWindow window, ILogger logger)
    {
        Window = window;
        Logger = logger;
        m_Stopwatch = new Stopwatch();
        m_Clock = new GameClock
        {
            Time = 0f,
            UpdateDeltaTime = 1f / 60f
        };
    }

    public void Start()
    {
        if (IsRunning)
            return;
        
        OnStart();
        IsRunning = true;
        while (IsRunning)
            Update();
    }

    public void Stop()
    {
        if (!IsRunning)
            return;

        IsRunning = false;
        OnStop();
        Stopped?.Invoke();
    }

    private void Update()
    {
        var deltaTimeTicks = m_Stopwatch.ElapsedTicks;
        m_Stopwatch.Restart();
        
        var frameTime = (double)deltaTimeTicks / Stopwatch.Frequency;
        m_FpsTime += frameTime;
        if (m_FpsTime >= 1)
        {
            //Logger.Trace($"FPS: {m_FrameCount}");
            m_FpsTime = 0;
            m_FrameCount = 0;
        } 
        
        if (frameTime > 0.25)
            frameTime = 0.25;

        m_Clock.FrameDeltaTime = (float)frameTime;
        m_Accumulator += frameTime;

        while (m_Accumulator >= m_DeltaTime)
        {
            Window.PollEvents();
            OnUpdate();
            m_Clock.Time += Clock.UpdateDeltaTime;
            m_Clock.OnTicked();
            m_Accumulator -= m_DeltaTime;
        }

        if (!Window.IsOpened)
            return;
        
        m_Clock.FrameLerpFactor = (float)m_Accumulator / m_DeltaTime;
        OnRender();
        Window.SwapBuffers();
        m_FrameCount++;
    }

    protected abstract void OnStart();
    protected abstract void OnUpdate();
    protected abstract void OnRender();
    protected abstract void OnStop();
}