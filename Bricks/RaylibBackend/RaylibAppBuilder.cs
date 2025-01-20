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

    public RaylibApp(string windowName, int windowWidth, int windowHeight)
    {
        Input = new RaylibInput();
        Raylib.SetTargetFPS(90);
        Raylib.InitWindow(windowWidth, windowHeight, windowName);
    }
    
    public void Update()
    {
    }

    public void Render(Paddle paddle)
    {
        Raylib.BeginDrawing();
        Raylib.ClearBackground(Color.Pink);

        var paddleWidth = paddle.Width;
        var paddleHeight = paddle.Height;
        var paddleHalfWidth = paddleWidth * 0.5f;
        var paddleHalfHeight = paddle.Height * 0.5f;
        var paddleCenterPosition = paddle.CenterPosition;
        var paddleLeft = paddleCenterPosition.X - paddleHalfWidth;
        var paddleTop = paddleCenterPosition.Y - paddleHalfHeight;
        Raylib.DrawRectangle((int)paddleLeft, (int)paddleTop, paddleWidth, paddleHeight, Color.Black);
        
        Raylib.EndDrawing();
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