using System.Numerics;
using EasyGameFramework.Api;
using OOPEcs;

namespace Tetris;

[Flags]
public enum Flag
{
    None = 0,
    Gravity = 1,
    Renderable
}

public sealed class TetrisGame : Game
{
    private SpriteRenderer m_SpriteRenderer;
    private TetrisRenderer m_TetrisRenderer;
    private ILogger m_Logger;
    
    public TetrisGame(IGameContext context, ILogger logger) : base(context)
    {
        m_Logger = logger;
        m_SpriteRenderer = new SpriteRenderer(context.Window);
        m_TetrisRenderer = new TetrisRenderer(logger, m_SpriteRenderer);
    }

    protected override void OnStartup()
    {
        Window.Title = "Tetris";
        Window.SetScreenSize(640, 480);
        m_SpriteRenderer.Load();
    }

    protected override void OnFixedUpdate()
    {
    }

    protected override void OnUpdate()
    {
        m_TetrisRenderer.Render(default, new TetrisSimState
        {
            PlayState = PlayState.Playing,
            StaticMonominoStates = new []
            {
                new MonominoState
                {
                    Position = new Vector2(0f, 10f),
                    Type = TetrominoType.I
                },
                new MonominoState
                {
                    Position = new Vector2(10f, 10f),
                    Type = TetrominoType.I
                },
            }
        });
        
        m_SpriteRenderer.Update();
    }

    protected override void OnShutdown()
    {
        m_SpriteRenderer.Unload();
    }
}