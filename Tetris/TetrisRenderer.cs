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
            CreateSprites(m_MoniminoSprites, delta);
        else if (delta < 0)
            DestroySprites(m_MoniminoSprites, -delta);
        
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
            CreateSprites(m_TetrominoSprites, delta);
        else if (delta < 0)
            DestroySprites(m_TetrominoSprites, -delta);

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
    
    private void CreateSprites(List<IRenderedSprite> sprites, int count)
    {
        for (var i = 0; i < count; i++)
        {
            var screenRect = new Rect(0, 0, m_GridSize - 2, m_GridSize - 2);
            var sprite = m_SpriteRenderer.Render(screenRect);
            sprites.Add(sprite);
        }
    }

    private void DestroySprites(List<IRenderedSprite> sprites, int count)
    {
        for (var i = sprites.Count - 1; i >= sprites.Count - count; i--)
        {
            var sprite = sprites[i];
            sprite.Dispose();
            sprites.RemoveAt(i);
        }
    }
}