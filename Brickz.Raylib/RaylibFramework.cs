using System.Numerics;
using Bricks.Archetypes;
using Bricks.Entities;
using Raylib_cs;

namespace Bricks.RaylibBackend;

internal sealed class RaylibFramework : IFramework
{
    public IKeyboard Keyboard { get; }
    
    private readonly Texture2D _spriteSheet;
    private readonly BrickzGame _game;

    public RaylibFramework(string windowName, int windowWidth, int windowHeight)
    {
        Keyboard = new RaylibKeyboard();
        Raylib.SetConfigFlags(ConfigFlags.VSyncHint);
        Raylib.InitWindow(windowWidth, windowHeight, windowName);
        _spriteSheet = Raylib.LoadTexture("Assets/sprite_atlas.png");
        _game = new BrickzGame(this);
    }

    public void Run()
    {
        _game.OnStartup();
        while (!Raylib.WindowShouldClose())
        {
            _game.OnUpdate();
            Render(_game.World);
        }
        _game.OnShutdown();
    }

    private void Render(World world)
    {
        Raylib.BeginDrawing();
        Raylib.ClearBackground(Color.DarkGray);

        DrawPaddle(world.Paddle);

        foreach (var ball in world.Balls.GetAll())
        {
            DrawBall(ball);
        }
        
        foreach (var brick in world.Bricks.GetAll())
        {
            DrawBrick(brick);
        }

        if (_game.State == GameState.Victory)
        {
            Raylib.DrawRectangle(0, 0, 640, 480, new Color(0f, 0f, 0f, 0.75f));
            var width = Raylib.MeasureText("Victory!", 50);
            Raylib.DrawText("Victory!", (int)(320 - width * 0.5f), 180, 50, Color.Green);
        }
        
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
            Color.White);
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
            Color.White);
    }

    private void DrawBrick(IBrick brick)
    {
        var brickRect = brick.GetAABB();
        if (brick.IsDamaged)
        {
            DrawDamagedBrickSprite(brickRect, Color.Blue);
        }
        else
        {
            DrawNormalBrickSprite(brickRect, Color.Blue);
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