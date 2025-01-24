using System.Numerics;
using Bricks.Archetypes;
using Bricks.Entities;
using EasyGameFramework.Api.InputDevices;
using EasyGameFramework.GUI;
using Raylib_CsLo;
using Color = Raylib_CsLo.Color;

namespace Bricks.RaylibBackend;

internal sealed class RaylibFramework : IFramework
{
    public IMouse Mouse => _mouse;
    public IKeyboard Keyboard => _keyboard;
    
    private readonly Texture _spriteSheet;
    private readonly BrickzGame _game;

    private readonly Color _white = new(255, 255, 255, 255);
    private readonly Color _brickColor = new(0, 121, 241, 255);

    private readonly RaylibGuiContext _guiRenderer;

    private readonly Widget _gui;

    private RaylibMouse _mouse;
    private RaylibKeyboard _keyboard;
    
    public RaylibFramework(string windowName, int windowWidth, int windowHeight)
    {
        _mouse = new RaylibMouse();
        _keyboard = new RaylibKeyboard();
        Raylib.SetConfigFlags(ConfigFlags.FLAG_VSYNC_HINT);
        Raylib.InitWindow(windowWidth, windowHeight, windowName);
        RayGui.GuiLoadStyleDefault();
        const int DEFAULT = 0;
        const int TEXT_SIZE = 16;
        RayGui.GuiSetStyle(DEFAULT, TEXT_SIZE, 20);
        _spriteSheet = Raylib.LoadTexture("Assets/sprite_atlas.png");
        _game = new BrickzGame(this);
        _guiRenderer = new RaylibGuiContext(_mouse, _keyboard);
        _gui = new GuiWidget(_game);
    }

    public void Run()
    {
        _game.OnStartup();
        while (!Raylib.WindowShouldClose())
        {
            _mouse.Update();
            _keyboard.Update();
            _game.OnUpdate();
            Render(_game.World);
        }
        _game.OnShutdown();
    }

    private void Render(World world)
    {
        Raylib.BeginDrawing();
        Raylib.ClearBackground(new Color(80, 80, 80, 255));

        DrawPaddle(world.Paddle);

        foreach (var ball in world.Balls.GetAll())
        {
            DrawBall(ball);
        }
        
        foreach (var brick in world.Bricks.GetAll())
        {
            DrawBrick(brick);
        }
        
        _gui.Update(_guiRenderer);
        _guiRenderer.Render();
        
        Raylib.EndDrawing();
    }

    private void DrawRectangle(AABB rect, Color color)
    {
        Raylib.DrawRectangle(
            (int)rect.Left, (int)rect.Top, 
            (int)rect.Width, (int)rect.Height,
            color
        );
    }

    private void DrawBall(IBall ball)
    {
        var ballRect = ball.GetAABB();
        DrawBallSprite(ballRect);
    }

    private void DrawBallSprite(AABB ballRect)
    {
        Raylib.DrawTexturePro(_spriteSheet,
            new Rectangle(120, 0, 20, 20),
            new Rectangle(ballRect.Left, ballRect.Top, ballRect.Width, ballRect.Height),
            new Vector2(0, 0),
            0, 
            _white);
    }

    private void DrawPaddle(PaddleEntity paddle)
    {
        var paddleRect = paddle.GetAABB();
        DrawPaddleSprite(paddleRect);
    }

    private void DrawPaddleSprite(AABB aabb)
    {
        Raylib.DrawTexturePro(_spriteSheet,
            new Rectangle(0, 0, 120, 19),
            new Rectangle(aabb.Left, aabb.Top, aabb.Width, aabb.Height),
            new Vector2(0, 0),
            0, 
            _white);
    }

    private void DrawBrick(IBrick brick)
    {
        var brickRect = brick.GetAABB();
        if (brick.IsDamaged)
        {
            DrawDamagedBrickSprite(brickRect, _brickColor);
        }
        else
        {
            DrawNormalBrickSprite(brickRect, _brickColor);
        }
    }

    private void DrawDamagedBrickSprite(AABB aabb, Color tint)
    {
        Raylib.DrawTexturePro(_spriteSheet,
            new Rectangle(0, 40, 60, 20),
            new Rectangle(aabb.Left, aabb.Top, aabb.Width, aabb.Height),
            new Vector2(0, 0),
            0, 
            tint);
    }

    private void DrawNormalBrickSprite(AABB aabb, Color tint)
    {
        Raylib.DrawTexturePro(_spriteSheet,
            new Rectangle(0, 20, 60, 20),
            new Rectangle(aabb.Left, aabb.Top, aabb.Width, aabb.Height),
            new Vector2(0, 0),
            0, 
            tint);
    }

    private void ReleaseUnmanagedResources()
    {
        Raylib.CloseWindow();
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    ~RaylibFramework()
    {
        ReleaseUnmanagedResources();
    }
}