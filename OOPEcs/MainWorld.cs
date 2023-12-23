using EasyGameFramework.Api;
using EasyGameFramework.Api.Events;
using EasyGameFramework.Api.InputDevices;
using OpenGLSandbox;

namespace Tetris;

public sealed class MainWorld : IEntity
{
    private World Context { get; } = new();
    
    public MainWorld(
        IWindow window,
        IClock clock,
        ITextRenderer textRenderer,
        ILogger logger
    )
    {
        //Context = new World(parentContext);
        Context.RegisterSingleton(window);
        Context.RegisterSingleton<ILogger>(logger);
        Context.RegisterSingleton(window.Input.Keyboard);
        Context.RegisterSingleton<ITextRenderer>(textRenderer);
        Context.RegisterSingleton<IClock>(clock);    
        Context.RegisterTransientEntity<SuperTestEntity>();
        Context.RegisterTransientEntity<QuitGameInputAction>();
    }

    public void Load()
    {
        Context.Load();
    }

    public void Unload()
    {
        Context.Unload();
    }
}

public sealed class QuitGameInputAction : IEntity
{
    private readonly IWindow m_Window;
    private readonly IClock m_Clock;
    private readonly ILogger m_Logger;
    private readonly ITextRenderer m_TextRenderer;

    private IRenderedText m_RenderedText;

    public QuitGameInputAction(IWindow window, IClock clock, ITextRenderer textRenderer, ILogger logger)
    {
        m_Window = window;
        m_TextRenderer = textRenderer;
        m_Logger = logger;
        m_Clock = clock;
    }

    public void Load()
    {
        m_Window.Input.Keyboard.KeyPressed += Keyboard_OnKeyPressed;
        m_RenderedText = m_TextRenderer.Render("Hello World!", new Rect(0, 0, 100, 100), new TextStyle
        {
            FontName = "test",
            Color = Color.FromHex(0xff00ff, 1f),
        });
        m_Clock.Ticked += Clock_OnTicked;
    }

    public void Unload()
    {
        m_Window.Input.Keyboard.KeyPressed -= Keyboard_OnKeyPressed;
    }

    private void Clock_OnTicked()
    {
        //m_Logger.Trace($"DT: {m_Clock.DeltaTime}");
        var screenRect = m_RenderedText.ScreenRect;
        screenRect.X += m_Clock.DeltaTime * 60f;
        m_RenderedText.ScreenRect = screenRect;
    }

    private void Keyboard_OnKeyPressed(in KeyboardKeyStateChangedEvent evt)
    {
        if (evt.Key == KeyboardKey.Escape)
        {
            m_Window.Close();
        }
    }
}