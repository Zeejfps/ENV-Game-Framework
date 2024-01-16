using System.Diagnostics;
using EasyGameFramework.Api;
using Entities;
using OpenGLSandbox;

namespace Tetris;

public sealed class TetrisRenderer
{
    private float m_GridSize = 40;
    private readonly IWindow m_Window;
    private readonly ILogger m_Logger;
    private readonly ISpriteRenderer m_SpriteRenderer;

    public TetrisRenderer(IWindow window, ILogger logger, ISpriteRenderer spriteRenderer)
    {
        m_Window = window;
        m_Logger = logger;
        m_SpriteRenderer = spriteRenderer;

        m_GridSize = window.ScreenHeight / 20f;
    }

    public void Render(TetrisSimState prevState, TetrisSimState currState)
    {
        if (currState.PlayState == PlayState.Playing)
        {
            RenderStaticMonominos(currState.StaticMonominoStates);
            RenderTetromino(currState.TetrominoState);
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
                m_MoniminoSprites.Add(m_SpriteRenderer.Render(new Rect(0, 0, m_GridSize - 2, m_GridSize - 2)));
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
            screenRect.X = state.Position.X * m_GridSize;
            screenRect.Y = (state.Position.Y - 0.5f) * m_GridSize;
            sprite.ScreenRect = screenRect;
        }
    }
    
    private readonly List<IRenderedSprite> m_TetrominoSprites = new();

    private void RenderTetromino(TetrominoState tetrominoState)
    {
        var offsets = tetrominoState.Offsets;
        var offsetsCount = offsets.Length;
        var delta = offsetsCount - m_TetrominoSprites.Count;
        if (delta > 0)
        {
            for (var i = 0; i < delta; i++)
            {
                m_TetrominoSprites.Add(m_SpriteRenderer.Render(new Rect(0, 0, m_GridSize - 2, m_GridSize - 2)));
            }
        }
        else if (delta < 0)
        {
            for (var i = m_TetrominoSprites.Count - 1; i >= m_TetrominoSprites.Count + delta; i--)
            {
                var sprite = m_TetrominoSprites[i];
                sprite.Dispose();
                m_TetrominoSprites.RemoveAt(i);
            }
        }

        var position = tetrominoState.Position;
        Debug.Assert(m_TetrominoSprites.Count == offsetsCount);
        for (var i = 0; i < offsetsCount; i++)
        {
            var offset = offsets[i];
            var sprite = m_TetrominoSprites[i];
            var screenRect = sprite.ScreenRect;
            screenRect.X = (position.X + offset.X) * m_GridSize;
            screenRect.Y = (position.Y + offset.Y - 0.5f) * m_GridSize;
            sprite.ScreenRect = screenRect;
        }
    }
    
}