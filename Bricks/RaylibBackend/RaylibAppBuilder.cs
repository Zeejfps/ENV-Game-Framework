﻿using Raylib_cs;

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
        Raylib.SetConfigFlags(ConfigFlags.VSyncHint);
        Raylib.InitWindow(windowWidth, windowHeight, windowName);
    }
    
    public void Update()
    {
    }

    public void Render(Paddle paddle, Ball ball)
    {
        Raylib.BeginDrawing();
        Raylib.ClearBackground(Color.DarkGray);

        var paddleRect = paddle.CalculateBoundsRectangle();
        Raylib.DrawRectangle(
            (int)paddleRect.Left, (int)paddleRect.Top, 
            (int)paddleRect.Width, (int)paddleRect.Height,
            Color.Black
        );
        
        var ballRect = ball.CalculateBoundsRectangle();
        Raylib.DrawRectangle(
            (int)ballRect.Left, (int)ballRect.Top, 
            (int)ballRect.Width, (int)ballRect.Height,
            Color.White
        );
        
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