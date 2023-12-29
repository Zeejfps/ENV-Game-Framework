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
    
    private Vector2 m_GridPosition;
    private IRenderedSprite? m_RenderedSprite;
    
    public void Load()
    {
        m_GridPosition = new Vector2(200, 400);
        m_RenderedSprite = m_SpriteRenderer.Render(
            screenRect: new Rect(
                m_GridPosition.X, m_GridPosition.Y,
                20, 20
            )
        );
        m_Clock.Ticked += Clock_OnTicked;
    }

    public void Unload()
    {
        m_Clock.Ticked -= Clock_OnTicked;
    }

    private void Clock_OnTicked()
    {
        m_Logger.Trace(m_GridPosition);   
        m_GridPosition -= Vector2.UnitY * m_Clock.DeltaTime;
        m_RenderedSprite.ScreenRect = new Rect(
            m_GridPosition.X, m_GridPosition.Y,
            20, 20
        );
    }
}