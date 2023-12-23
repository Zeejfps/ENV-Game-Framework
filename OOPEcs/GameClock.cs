using System.Diagnostics;
using EasyGameFramework.Api;
using Tetris;

namespace OOPEcs;

public sealed class GameClock : IClock, IEntity
{
    public event Action? Ticked;
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
       Tick();
    }

    private void Tick()
    {
        var deltaTimeTicks = m_Stopwatch.ElapsedTicks;
        DeltaTime = (float)deltaTimeTicks / Stopwatch.Frequency;
        m_Stopwatch.Restart();
        Ticked?.Invoke();
    }
}