using EasyGameFramework.Api;
using EasyGameFramework.Api.Events;
using EasyGameFramework.Api.InputDevices;
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
    private IWindow m_Window;
    private TetrisSim m_TetrisSim;
    
    public TetrisGame(IGameContext context, ILogger logger, IWindow window) : base(context)
    {
        m_Logger = logger;
        m_Window = window;
        m_SpriteRenderer = new SpriteRenderer(context.Window);
        m_TetrisRenderer = new TetrisRenderer(context.Window, logger, m_SpriteRenderer);
        m_TetrisSim = new TetrisSim();
    }

    protected override void OnStartup()
    {
        Window.Title = "Tetris";
        Window.SetScreenSize(640, 480);
        m_SpriteRenderer.Load();
        var keyboard = m_Window.Input.Keyboard;
        keyboard.KeyPressed += Keyboard_OnKeyPressed;
    }

    protected override void OnShutdown()
    {
        var keyboard = m_Window.Input.Keyboard;
        keyboard.KeyPressed -= Keyboard_OnKeyPressed;
        m_SpriteRenderer.Unload();
    }

    protected override void OnFixedUpdate()
    {
    }

    protected override void OnUpdate()
    {
        m_TetrisSim.Update(Time.UpdateDeltaTime);
        
        var state = m_TetrisSim.Save();
        m_TetrisRenderer.Render(default, state);
        
        glClearColor(0.1f, 0.4f,0.2f, 1f);
        glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);
        m_SpriteRenderer.Update();
    }

    private void Keyboard_OnKeyPressed(in KeyboardKeyStateChangedEvent evt)
    {
        if (evt.Key == KeyboardKey.D)
        {
            m_TetrisSim.MoveTetrominoRight();
        }
        else if (evt.Key == KeyboardKey.A)
        {
            m_TetrisSim.MoveTetrominoLeft();
        }
        else if (evt.Key == KeyboardKey.S)
        {
            m_TetrisSim.MoveTetrominoDown();
        }
    }
}