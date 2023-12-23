using System.Diagnostics;
using EasyGameFramework.Api;
using OpenGLSandbox;
using Tetris;

namespace OOPEcs;

public sealed class TestGameApp : WindowedApp
{
    private readonly IWindow m_Window;
    private readonly BitmapFontTextRenderer m_TextRenderer;
    
    public TestGameApp(IWindow window, ILogger logger) : base(window)
    {
        m_Window = window;
        m_TextRenderer = new BitmapFontTextRenderer(window);
        
        RegisterSingleton<ILogger>(logger);
        RegisterSingleton<IWindow>(window);

        RegisterSingleton<ITextRenderer>(m_TextRenderer);
        RegisterSingletonEntity<IClock, GameClock>();
        RegisterTransientEntity<MainWorld>();
    }

    public void Launch()
    {
        m_TextRenderer.Load(new BmpFontFile
        {
            FontName = "test",
            PathToFile = "Assets/bitmapfonts/Segoe UI.fnt"
        });
        
        Load();

        var window = m_Window;
        window.Title = "OOP ECS";
        window.Paint += Window_OnPaint;
        window.Closed += Window_OnClosed;
        window.OpenCentered();
    }

    private void Window_OnPaint()
    {
        m_TextRenderer.Update();
    }

    private void Window_OnClosed()
    {
        Unload();
    }
}

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