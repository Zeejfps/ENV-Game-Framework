using System.Diagnostics;
using EasyGameFramework.Core;

namespace EasyGameFramework.Api;

public abstract class Game : IApp
{
    public bool IsRunning { get; private set; }
    
    private readonly Stopwatch m_Stopwatch;
    private float m_DeltaTime = 1f / 60f;
    private double m_Accumulator = 0.0;
    private readonly GameClock m_Clock;

    protected IWindow Window { get; }
    protected IInput Input { get; }
    protected IClock Clock => m_Clock;
    
    protected Game(IWindow window, IInput input)
    {
        Window = window;
        Input = input;
        m_Stopwatch = new Stopwatch();
        m_Clock = new GameClock
        {
            Time = 0f,
            DeltaTime = 1f / 60f
        };
    }
    
    public void Setup()
    {
        IsRunning = true;
        OnSetup();
    }
    
    public void Update()
    {
        Window.Update();

        if (!Window.IsOpened)
        {
            Quit();
            return;
        }
        
        var deltaTimeTicks = m_Stopwatch.ElapsedTicks;
        m_Stopwatch.Restart();
        
        var frameTime = (double)deltaTimeTicks / Stopwatch.Frequency;
        if (frameTime > 0.25)
            frameTime = 0.25;

        m_Accumulator += frameTime;

        while (m_Accumulator >= m_DeltaTime)
        {
            OnUpdate();
            Input.Update();
            m_Clock.Time += Clock.DeltaTime;
            m_Accumulator -= m_DeltaTime;
        }

        var alpha = (float)m_Accumulator / m_DeltaTime;
        OnRender();
    }

    public void Quit()
    {
        IsRunning = false;
        OnTeardown();
    }

    protected abstract void OnSetup();
    protected abstract void OnUpdate();
    protected abstract void OnRender();
    protected abstract void OnTeardown();
}