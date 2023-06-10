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

    protected IContext Context { get; }
    protected IGameTime Time => m_Time;
    protected ILogger Logger { get; }

    private float m_DeltaTime = 1f / 60f;
    private double m_Accumulator = 0.0;
    
    private readonly Stopwatch m_Stopwatch;
    private readonly GameTime m_Time;

    private double m_FpsTime;
    private int m_FrameCount;
    
    protected Game(IContext context)
    {
        Context = context;
        Window = context.Window;
        Logger = context.Logger;
        m_Stopwatch = new Stopwatch();
        m_Time = new GameTime
        {
            Time = 0f,
            UpdateDeltaTime = 1f / 60f
        };
    }

    public void Run()
    {
        if (IsRunning)
            return;

        Configure();
        
        var window = Window;
        window.Closed += Window_OnClosed;
        window.OpenCentered();
        
        OnStart();
        IsRunning = true;
        while (IsRunning)
            Update();
        
        OnStop();
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
        var deltaTimeTicks = m_Stopwatch.ElapsedTicks;
        m_Stopwatch.Restart();
        
        var frameTime = (double)deltaTimeTicks / Stopwatch.Frequency;
        m_FpsTime += frameTime;
        if (m_FpsTime >= 5)
        {
            var fps = m_FrameCount / 5;
            Logger.Trace($"FPS: {fps}");
            m_FpsTime = 0;
            m_FrameCount = 0;
        } 
        
        if (frameTime > 0.25)
            frameTime = 0.25;

        m_Time.FrameDeltaTime = (float)frameTime;
        m_Accumulator += frameTime;

        var window = Window;
        while (m_Accumulator >= m_DeltaTime)
        {
            window.PollEvents();
            if (!IsRunning)
                return;
            
            OnUpdate();
            m_Time.Time += Time.UpdateDeltaTime;
            m_Accumulator -= m_DeltaTime;
        }

        if (!IsRunning || !window.IsOpened)
            return;
        
        m_Time.FrameLerpFactor = (float)m_Accumulator / m_DeltaTime;
        OnRender();
        window.SwapBuffers();
        m_FrameCount++;
    }

    protected abstract void Configure();
    protected abstract void OnStart();
    protected abstract void OnUpdate();
    protected abstract void OnRender();
    protected abstract void OnStop();
}