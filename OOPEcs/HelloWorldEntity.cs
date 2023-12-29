using EasyGameFramework.Api;
using OpenGLSandbox;

namespace Tetris;

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
        var fontFamily = "test";
        var text = "Hello World!";
        var textWidth = m_TextRenderer.CalculateTextWidth(text, fontFamily);
        m_RenderedText = m_TextRenderer.Render(
            text: text, 
            fontFamily: fontFamily,
            screenPosition: new Rect(0, m_Window.ScreenHeight - 80, textWidth, 50), 
            style: new TextStyle
            {
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