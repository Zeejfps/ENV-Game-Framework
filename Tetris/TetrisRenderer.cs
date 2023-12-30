using System.Diagnostics;
using EasyGameFramework.Api;
using Entities;
using OpenGLSandbox;

namespace Tetris;

public sealed class TetrisRenderer
{
    private readonly IWindow m_Window;
    private readonly ILogger m_Logger;
    private readonly ISpriteRenderer m_SpriteRenderer;

    public TetrisRenderer(IWindow window, ILogger logger, ISpriteRenderer spriteRenderer)
    {
        m_Window = window;
        m_Logger = logger;
        m_SpriteRenderer = spriteRenderer;
    }

    public void Render(TetrisSimState prevState, TetrisSimState currState)
    {
        if (currState.PlayState == PlayState.Playing)
        {
            RenderStaticMonominos(currState.StaticMonominoStates);
        }
    }

    private readonly List<IRenderedSprite> m_MoniminoSprites = new();
    private void RenderStaticMonominos(MonominoState[] staticMonomino)
    {
        var delta = staticMonomino.Length - m_MoniminoSprites.Count;
        if (delta > 0)
        {
            for (var i = 0; i < delta; i++)
            {
                m_MoniminoSprites.Add(m_SpriteRenderer.Render(new Rect(0, 0, 48f, 48f)));
            }
        }
        else if (delta < 0)
        {
            m_MoniminoSprites.RemoveRange(m_MoniminoSprites.Count - delta, delta);
        }
        
        Debug.Assert(m_MoniminoSprites.Count == staticMonomino.Length);
        for (var i = 0; i < staticMonomino.Length; i++)
        {
            var state = staticMonomino[i];
            var sprite = m_MoniminoSprites[i];
            var screenRect = sprite.ScreenRect;
            screenRect.X = state.Position.X * 50f;
            screenRect.Y = state.Position.Y * 50f;
            sprite.ScreenRect = screenRect;
        }
    }
}