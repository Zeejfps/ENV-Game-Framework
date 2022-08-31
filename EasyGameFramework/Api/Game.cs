using System.Diagnostics;

namespace EasyGameFramework.Api;

public abstract class Game : IApp
{
    public bool IsRunning { get; private set; }
    
    private readonly Stopwatch m_Stopwatch;
    private float m_DeltaTime = 1f / 60f;
    private double m_Accumulator = 0.0;

    protected IWindow Window { get; }
    protected IInput Input { get; }
    
    protected Game(IWindow window, IInput input)
    {
        Window = window;
        Input = input;
        m_Stopwatch = new Stopwatch();
    }
    
    public void Start()
    {
        IsRunning = true;
        OnStart();
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
            OnUpdate(m_DeltaTime);
            Input.Update();
            m_Accumulator -= m_DeltaTime;
        }

        OnRender((float)m_Accumulator / m_DeltaTime);
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