using System.Numerics;
using EasyGameFramework.Api;
using OpenGLSandbox;
using Tetris;

namespace Entities;

public sealed class Monomino : IEntity
{     
    private readonly IClock m_Clock;
    private readonly ILogger m_Logger;
    private readonly ISpriteRenderer m_SpriteRenderer;
    
    public Monomino(IClock clock, ISpriteRenderer spriteRenderer, ILogger logger)
    {
        m_Clock = clock;
        m_SpriteRenderer = spriteRenderer;
        m_Logger = logger;
    }
    
    private Rect m_ScreenRect;

    public Vector2 GridPos
    {
        get;
        set;
    }
    
    private IRenderedSprite? m_RenderedSprite;
    
    public void Load()
    {
        m_ScreenRect = new Rect(200, 400, 50, 50);
        m_RenderedSprite = m_SpriteRenderer.Render(
            screenRect: m_ScreenRect
        );
        m_Clock.Ticked += Clock_OnTicked;
    }

    public void Unload()
    {
        m_Clock.Ticked -= Clock_OnTicked;
    }

    private void Clock_OnTicked()
    {
        m_ScreenRect.BottomLeft -= Vector2.UnitY * m_Clock.DeltaTime * 30;
        m_RenderedSprite.ScreenRect = m_ScreenRect;
    }

    public void MoveDown()
    {
        throw new NotImplementedException();
    }
}