using System.Diagnostics;
using EasyGameFramework.Api;
using Tetris;

namespace OOPEcs;

public sealed class GameClock : IClock, IEntity
{
    public event Action? Ticked;
    public float Time { get; }
    public float DeltaTime { get; private set; }

    private readonly IWindow m_Window;
    private readonly ILogger m_Logger;
    private readonly Stopwatch m_Stopwatch;

    public GameClock(IWindow window, ILogger logger)
    {
        m_Window = window;
        m_Logger = logger;
        m_Stopwatch = new Stopwatch();
    }

    public void Load()
    {
        m_Window.Paint += Window_OnPaint;
    }

    public void Unload()
    {
        m_Window.Paint -= Window_OnPaint;
    }

    private void Window_OnPaint()
    {
        var deltaTimeTicks = m_Stopwatch.ElapsedTicks;
        var frameTime = (double)deltaTimeTicks / Stopwatch.Frequency;
        m_Stopwatch.Restart();
        Tick((float)frameTime);
    }

    public void Tick(float dt)
    {
        DeltaTime = dt;
        Ticked?.Invoke();
    }
}