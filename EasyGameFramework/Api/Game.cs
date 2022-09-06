using System.Diagnostics;
using EasyGameFramework.Core;

namespace EasyGameFramework.Api;

public abstract class Game : IApp
{
    public bool IsRunning { get; private set; }

    private float m_DeltaTime = 1f / 60f;
    private double m_Accumulator = 0.0;
    
    private readonly Stopwatch m_Stopwatch;
    private readonly GameClock m_Clock;

    protected IWindow Window { get; }
    protected IInputSystem Input { get; }
    protected IGameClock Clock => m_Clock;
    
    protected Game(IWindow window, IInputSystem input)
    {
        Window = window;
        Input = input;
        m_Stopwatch = new Stopwatch();
        m_Clock = new GameClock
        {
            Time = 0f,
            UpdateDeltaTime = 1f / 60f
        };
    }
    
    public void Run()
    {
        Start();
        while (IsRunning)
        {
            Update();
        }
    }
    
    private void Start()
    {
        IsRunning = true;
        OnStart();
    }
    
    private void Update()
    {
        Window.PollEvents();

        if (!Window.IsOpened)
        {
            Stop();
            return;
        }
        
        var deltaTimeTicks = m_Stopwatch.ElapsedTicks;
        m_Stopwatch.Restart();
        
        var frameTime = (double)deltaTimeTicks / Stopwatch.Frequency;
        if (frameTime > 0.25)
            frameTime = 0.25;

        m_Clock.FrameDeltaTime = (float)frameTime;
        m_Accumulator += frameTime;

        while (m_Accumulator >= m_DeltaTime)
        {
            OnUpdate();
            m_Clock.Time += Clock.UpdateDeltaTime;
            m_Accumulator -= m_DeltaTime;
        }

        m_Clock.FrameLerpFactor = (float)m_Accumulator / m_DeltaTime;
        OnRender();
        Window.SwapBuffers();
    }

    public void Stop()
    {
        IsRunning = false;
        OnStop();
    }

    protected abstract void OnStart();
    protected abstract void OnUpdate();
    protected abstract void OnRender();
    protected abstract void OnStop();
}