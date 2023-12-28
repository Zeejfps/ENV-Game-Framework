using EasyGameFramework.Api;
using EasyGameFramework.Api.Events;
using EasyGameFramework.Api.InputDevices;
using OpenGLSandbox;

namespace Tetris;

public sealed class MainWorld : IEntity
{
    private EntityContext Context { get; } = new();
    
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

public sealed class HelloWorldEntity : IEntity
{
    private readonly IWindow m_Window;
    private readonly IClock m_Clock;
    private readonly ILogger m_Logger;
    private readonly ITextRenderer m_TextRenderer;

    private float m_Phase;
    private IRenderedText m_RenderedText;

    public HelloWorldEntity(IWindow window, IClock clock, ILogger logger, ITextRenderer textRenderer)
    {
        m_Clock = clock;
        m_Logger = logger;
        m_TextRenderer = textRenderer;
        m_Window = window;
    }

    public void Load()
    {
        var fontName = "test";
        var text = "Hello World!";
        var textWidth = m_TextRenderer.CalculateTextWidth(text, fontName);
        m_RenderedText = m_TextRenderer.Render(
            text: text, 
            screenPosition: new Rect(0, m_Window.ScreenHeight - 80, textWidth, 50), 
            style: new TextStyle
            {
                FontName = fontName,
                Color = Color.FromHex(0xff00ff, 1f),
            }
        );
        m_Clock.Ticked += Clock_OnTicked;
    }

    public void Unload()
    {
        
    }

    private float m_Direction = 1f;
    
    private void Clock_OnTicked()
    {
        //m_Logger.Trace($"DT: {m_Clock.DeltaTime}");
        var screenRect = m_RenderedText.ScreenRect;
        if (screenRect.Right >= m_Window.ScreenWidth)
            m_Direction = -1f;
        else if (screenRect.Left <= 0f)
            m_Direction = 1f;
        
        var xMovement = m_Clock.DeltaTime * 60f * m_Direction;
        screenRect.X += xMovement ;
        
        m_Phase += m_Clock.DeltaTime * 5f;
        screenRect.Y += MathF.Sin(m_Phase) * 0.01f;
        m_RenderedText.ScreenRect = screenRect;
    }
}

public sealed class QuitGameInputAction : IEntity
{
    private readonly IWindow m_Window;
    private readonly ILogger m_Logger;


    public QuitGameInputAction(IWindow window, ILogger logger)
    {
        m_Window = window;
        m_Logger = logger;
    }

    public void Load()
    {
        m_Window.Input.Keyboard.KeyPressed += Keyboard_OnKeyPressed;
    }

    public void Unload()
    {
        m_Window.Input.Keyboard.KeyPressed -= Keyboard_OnKeyPressed;
    }
    
    private void Keyboard_OnKeyPressed(in KeyboardKeyStateChangedEvent evt)
    {
        if (evt.Key == KeyboardKey.Escape)
        {
            m_Window.Close();
        }
    }
}