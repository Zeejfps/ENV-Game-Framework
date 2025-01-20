using System.Numerics;
using Raylib_cs;

namespace Bricks.RaylibBackend;

public sealed class RaylibAppBuilder : IAppBuilder
{
    private string _windowName;
    private int _canvasWidth;
    private int _canvasHeight;
    
    public void WithWindowName(string brickz)
    {
        _windowName = brickz;
    }

    public void WithCanvasSize(int width, int height)
    {
        _canvasWidth = width;
        _canvasHeight = height;
    }

    public IApp Build()
    {
        return new RaylibApp(_windowName, _canvasWidth, _canvasHeight);
    }
}

internal sealed class RaylibApp : IApp
{
    public bool IsCloseRequested => Raylib.WindowShouldClose();
    public IInput Input { get; }
    
    private readonly Texture2D _spriteSheet;

    public RaylibApp(string windowName, int windowWidth, int windowHeight)
    {
        Input = new RaylibInput();
        Raylib.SetConfigFlags(ConfigFlags.VSyncHint);
        Raylib.InitWindow(windowWidth, windowHeight, windowName);
        _spriteSheet = Raylib.LoadTexture("Assets/sprite_atlas.png");
    }
    
    public void Update()
    {
    }

    public void Render(Paddle paddle, Ball ball, BricksRepo bricks)
    {
        Raylib.BeginDrawing();
        Raylib.ClearBackground(Color.DarkGray);

        DrawPaddle(paddle);
        DrawBall(ball);
        
        foreach (var brick in bricks.GetAll())
        {
            DrawBrick(brick);
        }
        
        Raylib.EndDrawing();
    }

    private void DrawRectangle(Rectangle rect, Color color)
    {
        Raylib.DrawRectangle(
            (int)rect.Left, (int)rect.Top, 
            (int)rect.Width, (int)rect.Height,
            color
        );
    }

    private void DrawBall(Ball ball)
    {
        var ballRect = ball.CalculateBoundsRectangle();
        DrawBallSprite(ballRect);
    }

    private void DrawBallSprite(Rectangle ballRect)
    {
        Raylib.DrawTexturePro(_spriteSheet,
            new Raylib_cs.Rectangle(120, 0, 20, 20),
            new Raylib_cs.Rectangle(ballRect.Left, ballRect.Top, ballRect.Width, ballRect.Height),
            new Vector2(0, 0),
            0, 
            Color.White);
    }

    private void DrawPaddle(Paddle paddle)
    {
        var paddleRect = paddle.CalculateBoundsRectangle();
        DrawPaddleSprite(paddleRect);
    }

    private void DrawPaddleSprite(Rectangle aabb)
    {
        Raylib.DrawTexturePro(_spriteSheet,
            new Raylib_cs.Rectangle(0, 0, 120, 19),
            new Raylib_cs.Rectangle(aabb.Left, aabb.Top, aabb.Width, aabb.Height),
            new Vector2(0, 0),
            0, 
            Color.White);
    }

    private void DrawBrick(IBrick brick)
    {
        var brickRect = brick.CalculateBoundsRectangle();
        if (brick.IsDamaged)
        {
            DrawDamagedBrickSprite(brickRect, Color.Blue);
        }
        else
        {
            DrawNormalBrickSprite(brickRect, Color.Blue);
        }
    }
    
    private void DrawDamagedBrickSprite(Rectangle aabb, Color tint)
    {
        Raylib.DrawTexturePro(_spriteSheet,
            new Raylib_cs.Rectangle(0, 40, 60, 20),
            new Raylib_cs.Rectangle(aabb.Left, aabb.Top, aabb.Width, aabb.Height),
            new Vector2(0, 0),
            0, 
            tint);
    }
    
    private void DrawNormalBrickSprite(Rectangle aabb, Color tint)
    {
        Raylib.DrawTexturePro(_spriteSheet,
            new Raylib_cs.Rectangle(0, 20, 60, 20),
            new Raylib_cs.Rectangle(aabb.Left, aabb.Top, aabb.Width, aabb.Height),
            new Vector2(0, 0),
            0, 
            tint);
    }
    
    public void Dispose()
    {
        Raylib.CloseWindow();
    }
}

internal sealed class RaylibInput : IInput
{
    public bool IsKeyDown(KeyCode keyCode)
    {
        var keyboardKey = ConvertKeyCodeToKeyboardKey(keyCode);
        return Raylib.IsKeyDown(keyboardKey);
    }

    private KeyboardKey ConvertKeyCodeToKeyboardKey(KeyCode keyCode)
    {
        return keyCode switch
        {
            KeyCode.A => KeyboardKey.A,
            KeyCode.D => KeyboardKey.D,
            _ => throw new ArgumentOutOfRangeException(nameof(keyCode), keyCode, null)
        };
    }
}