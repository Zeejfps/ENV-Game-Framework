using System.Diagnostics;
using EasyGameFramework.Api.Rendering;
using EasyGameFramework.Core;

namespace EasyGameFramework.Api;

public abstract class Game : IGame
{
    public bool IsRunning { get; private set; }
    
    public IWindow Window { get; }
    public IGpu Gpu => Window.Gpu;
    public IInputSystem Input => Window.Input;

    protected IGameContext GameContext { get; }
    protected IGameTime Time => m_Time;
    protected ILogger Logger { get; }

    private double m_Accumulator = 0.0;
    
    private readonly Stopwatch m_Stopwatch;
    private readonly GameTime m_Time;

    private double m_FpsTime;
    private int m_FrameCount;
    
    protected Game(IGameContext gameContext)
    {
        GameContext = gameContext;
        Window = gameContext.Window;
        Logger = gameContext.Logger;
        m_Stopwatch = new Stopwatch();
        m_Time = new GameTime
        {
            Time = 0f,
            FixedUpdateDeltaTime = 1 / 60f
        };
    }

    public void Launch()
    {
        if (IsRunning)
            return;

        OnStartup();
        
        IsRunning = true;
        var window = Window;
        window.Closed += Window_OnClosed;
        window.Paint += Update;
        window.OpenCentered();
        OnShutdown();
    }

    public void Exit()
    {
        if (!IsRunning)
            return;
        IsRunning = false;
    }

    private void Window_OnClosed()
    {
        Window.Closed -= Window_OnClosed;
        Exit();
    }

    private void Update()
    {
        var deltaTime = Time.FixedUpdateDeltaTime;
        var deltaTimeTicks = m_Stopwatch.ElapsedTicks;
        m_Stopwatch.Restart();
        
        var frameTime = (double)deltaTimeTicks / Stopwatch.Frequency;
        m_FpsTime += frameTime;
        if (m_FpsTime >= 1)
        {
            var fps = m_FrameCount;
            Logger.Trace($"FPS: {fps}");
            m_FpsTime = 0;
            m_FrameCount = 0;
        } 
        
        if (frameTime > 0.25)
            frameTime = 0.25;

        m_Time.UpdateDeltaTime = (float)frameTime;
        m_Accumulator += frameTime;

        var window = Window;

        while (m_Accumulator >= deltaTime)
        {
            if (!IsRunning)
                return;
            
            OnFixedUpdate();
            m_Time.Time += Time.FixedUpdateDeltaTime;
            m_Accumulator -= deltaTime;
        }

        if (!IsRunning || !window.IsOpened)
            return;
        
        m_Time.FrameLerpFactor = (float)m_Accumulator / deltaTime;
        OnBeginFrame();
        OnUpdate();
        OnEndFrame();
        m_FrameCount++;
    }

    protected virtual void OnBeginFrame(){}
    protected virtual void OnEndFrame(){}
    protected abstract void OnStartup();
    protected abstract void OnFixedUpdate();
    protected abstract void OnUpdate();
    protected abstract void OnShutdown();
}