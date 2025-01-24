using System.Numerics;
using Bricks.Archetypes;
using Bricks.Entities;
using EasyGameFramework.GUI;
using OpenGLSandbox;
using Raylib_CsLo;
using Color = Raylib_CsLo.Color;

namespace Bricks.RaylibBackend;

public sealed class GuiWidget : StatefulWidget
{
    private readonly BrickzGame _game;

    public GuiWidget(BrickzGame game)
    {
        _game = game;
        _game.StateChanged += SetDirty;
    }
    
    protected override IWidget Build(IBuildContext context)
    {
        if (_game.State == GameState.Victory)
        {
            return new Column
            {
                ScreenRect = new Rect(100, 0, 440, 480),
                Spacing = 10,
                Children =
                {
                    new TextWidget("Victory!")
                    {
                        Style = new TextStyle
                        {
                            Color = new OpenGLSandbox.Color(0f, 1f, 0f, 1f),
                            HorizontalTextAlignment = TextAlignment.Center,
                            VerticalTextAlignment = TextAlignment.Center,
                        }
                    },
                    new PanelWidget
                    {
                        Style = new PanelStyle
                        {
                            BackgroundColor = new OpenGLSandbox.Color(1f, 0f, 1f, 1f),
                        }
                    }
                }
            };
        }

        if (_game.State == GameState.Defeat)
        {
            return new Column
            {
                ScreenRect = new Rect(100, 0, 440, 480),
                Spacing = 10,
                Children =
                {
                    new TextWidget("DEFEAT :(")
                    {
                        Style = new TextStyle
                        {
                            FontScale = 50,
                            Color = new OpenGLSandbox.Color(1f, 0f, 0f, 1f),
                            HorizontalTextAlignment = TextAlignment.Center,
                            VerticalTextAlignment = TextAlignment.Center,
                        }
                    }
                }
            };
        }
        return null;
    }
}

internal sealed class RaylibFramework : IFramework
{
    public IKeyboard Keyboard { get; }
    
    private readonly Texture _spriteSheet;
    private readonly BrickzGame _game;

    private readonly Color _white = new(255, 255, 255, 255);
    private readonly Color _brickColor = new(0, 121, 241, 255);

    private readonly RaylibGuiContext _guiContext;

    private readonly Widget _gui;
    
    public RaylibFramework(string windowName, int windowWidth, int windowHeight)
    {
        Keyboard = new RaylibKeyboard();
        Raylib.SetConfigFlags(ConfigFlags.FLAG_VSYNC_HINT);
        Raylib.InitWindow(windowWidth, windowHeight, windowName);
        RayGui.GuiLoadStyleDefault();
        const int DEFAULT = 0;
        const int TEXT_SIZE = 16;
        RayGui.GuiSetStyle(DEFAULT, TEXT_SIZE, 20);
        _spriteSheet = Raylib.LoadTexture("Assets/sprite_atlas.png");
        _game = new BrickzGame(this);
        _guiContext = new RaylibGuiContext();
        _gui = new GuiWidget(_game);
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

        if (_game.State == GameState.Victory)
        {
            DrawVictoryScreen();
        }
        else if (_game.State == GameState.Paused)
        {
            DrawPauseScreen();
        }
        else if (_game.State == GameState.Defeat)
        {
            DrawDefeatScreen();
        }
        
        _gui.Update(_guiContext);
        _guiContext.Render();
        
        Raylib.EndDrawing();
    }

    private void DrawDefeatScreen()
    {
        var victoryText = "DEFEAT :(";
        var fontSize = 50;
        Raylib.DrawRectangle(0, 0, 640, 480, new Color(0, 0, 0, 200));
        var width = Raylib.MeasureText(victoryText, fontSize);
        Raylib.DrawText(victoryText, (int)(320 - width * 0.5f), 180, fontSize, new Color(255, 50, 0, 255));
        
        var buttonPosition = new Rectangle(320 - 100, 250, 200, 40);
        var restartButtonClicked = RayGui.GuiButton(buttonPosition, "restart");
        if (restartButtonClicked)
        {
            _game.Restart();
        }
    }

    private void DrawVictoryScreen()
    {
        var victoryText = "VICTORY!";
        var fontSize = 50;
        Raylib.DrawRectangle(0, 0, 640, 480, new Color(0, 0, 0, 200));
        var width = Raylib.MeasureText(victoryText, fontSize);
        Raylib.DrawText(victoryText, (int)(320 - width * 0.5f), 180, fontSize, new Color(0, 255, 0, 255));
        
        var buttonPosition = new Rectangle(320 - 100, 250, 200, 40);
        var restartButtonClicked = RayGui.GuiButton(buttonPosition, "restart");
        if (restartButtonClicked)
        {
            _game.Restart();
        }
    }

    private void DrawPauseScreen()
    {
        var text = "PAUSED";
        var fontSize = 50;
        Raylib.DrawRectangle(0, 0, 640, 480, new Color(0, 0, 0, 200));
        var width = Raylib.MeasureText(text, fontSize);
        Raylib.DrawText(text, (int)(320 - width * 0.5f), 180, fontSize, new Color(0, 255, 0, 255));
        
        var buttonPosition = new Rectangle(320 - 100, 250, 200, 40);
        var buttonClicked = RayGui.GuiButton(buttonPosition, "resume");
        if (buttonClicked)
        {
            _game.Resume();
        }
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