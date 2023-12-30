using System.Numerics;
using EasyGameFramework.Api;
using OOPEcs;
using static OpenGL.Gl;

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
        m_TetrisRenderer = new TetrisRenderer(context.Window, logger, m_SpriteRenderer);
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
                    Position = new Vector2(2f, 2),
                    Type = TetrominoType.I
                },
                new MonominoState
                {
                    Position = new Vector2(3, 2),
                    Type = TetrominoType.I
                },
                new MonominoState
                {
                    Position = new Vector2(4, 2),
                    Type = TetrominoType.I
                },
                new MonominoState
                {
                    Position = new Vector2(3, 3),
                    Type = TetrominoType.I
                },
            }
        });
        
        glClearColor(0.1f, 0.4f,0.2f, 1f);
        glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);
        m_SpriteRenderer.Update();
    }

    protected override void OnShutdown()
    {
        m_SpriteRenderer.Unload();
    }
}