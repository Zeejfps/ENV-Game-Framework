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
            UpdateDeltaTime = 1 / 60f
        };
    }

    public void Launch()
    {
        if (IsRunning)
            return;

        OnStartup();
        
        var window = Window;
        window.Closed += Window_OnClosed;
        window.OpenCentered();
        
        IsRunning = true;
        while (IsRunning)
            Update();
        
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
        var deltaTime = Time.UpdateDeltaTime;
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

        m_Time.FrameDeltaTime = (float)frameTime;
        m_Accumulator += frameTime;

        var window = Window;
        while (m_Accumulator >= deltaTime)
        {
            window.PollEvents();
            if (!IsRunning)
                return;
            
            OnFixedUpdate();
            m_Time.Time += Time.UpdateDeltaTime;
            m_Accumulator -= deltaTime;
        }

        if (!IsRunning || !window.IsOpened)
            return;
        
        m_Time.FrameLerpFactor = (float)m_Accumulator / deltaTime;
        OnUpdate();
        window.SwapBuffers();
        m_FrameCount++;
    }

    protected abstract void OnStartup();
    protected abstract void OnFixedUpdate();
    protected abstract void OnUpdate();
    protected abstract void OnShutdown();
}