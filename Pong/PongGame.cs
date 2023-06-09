using System.Numerics;
using EasyGameFramework.Api;
using EasyGameFramework.Api.Cameras;
using EasyGameFramework.Api.InputDevices;
using EasyGameFramework.Api.Physics;

namespace Pong;

public sealed class PongGame : Game
{
    private IInputSystem InputSystem => Window.Input;
    private SpriteRenderer SpriteRenderer { get; }
    private ICamera Camera { get; }
    private Sprite PaddleSprite { get; set; }
    //private Sprite BallSprite { get; set; }

    private Rect LevelBounds = new()
    {
        Position = new Vector2(-50, -50),
        Width = 100,
        Height = 100,
    };
    
    private Paddle BottomPaddle { get; }
    private Paddle TopPaddle { get; }
    private Ball Ball { get; }

    private BallPaddleCollisionSystem BallPaddleCollisionSystem { get; } = new BallPaddleCollisionSystem();

    public PongGame(IWindow window, ILogger logger) : base(window, logger)
    {
        SpriteRenderer = new SpriteRenderer(Gpu);
        Camera = OrthographicCamera.FromLRBT(
            LevelBounds.Left, LevelBounds.Right, 
            LevelBounds.Bottom, LevelBounds.Top, 
            0.01f, 10f);
        
        BottomPaddle = new()
        {
            CurrPosition = new Vector2(0, -40f),
            PrevPosition = new Vector2(0f, -40f),
            Bounds = LevelBounds
        };
    
        TopPaddle = new()
        {
            CurrPosition = new Vector2(0, 40f),
            PrevPosition = new Vector2(0f, 40f),
            Bounds = LevelBounds
        };

        Ball = new Ball(logger)
        {
            Velocity = new Vector2(10, 30),
            Bounds = LevelBounds
        };
    }

    protected override void Configure()
    {
        var window = Window;
        window.Title = "Pong";
        window.IsResizable = true;
        window.IsVsyncEnabled = true;
        window.CursorMode = CursorMode.Visible;
        window.SetViewportSize(640, 640);
    }

    protected override void OnStart()
    {
        var texture = Gpu.Texture.Load("Assets/white");
        
        PaddleSprite = new Sprite
        {
            Size = new Vector2(32f, 32f),
            Texture = texture
        };
        SpriteRenderer.LoadResources();
    }

    protected override void OnUpdate()
    {
        var keyboard = InputSystem.Keyboard;
        if (keyboard.IsKeyPressed(KeyboardKey.Escape))
        {
            Exit();
            return;
        }

        Ball.Update(Time.UpdateDeltaTime);
        
        var paddleSpeed = 30f;
        var paddlePositionDelta = Time.UpdateDeltaTime * paddleSpeed;
        
        BottomPaddle.PrevPosition = BottomPaddle.CurrPosition;
        if (keyboard.IsKeyPressed(KeyboardKey.A))
            BottomPaddle.MoveLeft(paddlePositionDelta);
        else if (keyboard.IsKeyPressed(KeyboardKey.D))
            BottomPaddle.MoveRight(paddlePositionDelta);
        
        TopPaddle.PrevPosition = TopPaddle.CurrPosition;
        if (keyboard.IsKeyPressed(KeyboardKey.LeftArrow))
            TopPaddle.MoveLeft(paddlePositionDelta);
        else if (keyboard.IsKeyPressed(KeyboardKey.RightArrow))
            TopPaddle.MoveRight(paddlePositionDelta);
        
        BallPaddleCollisionSystem.Update(Ball, BottomPaddle, TopPaddle);
    }

    protected override void OnRender()
    {
        var camera = Camera;
        var gpu = Gpu;

        var viewportWidth = Window.ViewportWidth;
        var viewportHeight = Window.ViewportHeight;
        var fb = gpu.CreateRenderbuffer(1, false, 1024, 1024);
        
        gpu.Renderbuffer.Bind(fb);
        gpu.Renderbuffer.ClearColorBuffers(0f, 0.3f, 0f, 1f);

        var frameLerpFactor = Time.FrameLerpFactor;
        SpriteRenderer.NewBatch();
        {
            var paddle1Pos = Vector2.Lerp(BottomPaddle.PrevPosition, BottomPaddle.CurrPosition, frameLerpFactor);
            SpriteRenderer.DrawSprite(paddle1Pos,  new Vector2(10f, 1f), PaddleSprite, Vector3.One);

            var paddle2Pos = Vector2.Lerp(TopPaddle.PrevPosition, TopPaddle.CurrPosition, frameLerpFactor);
            SpriteRenderer.DrawSprite(paddle2Pos,  new Vector2(10f, 1f), PaddleSprite, Vector3.One);

            var ballPosition = Vector2.Lerp(Ball.PrevPosition, Ball.CurrPosition, frameLerpFactor);
            SpriteRenderer.DrawSprite(ballPosition,  new Vector2(1f, 1f), PaddleSprite, new Vector3(0.5f, 0.7f, 0.1f));
        }
        SpriteRenderer.RenderBatch(camera);

        var aspect = 1f;
        var min = (int)MathF.Min(viewportWidth, viewportHeight);
        var x = (int)MathF.Round(viewportWidth * 0.5f - min * 0.5f);
        var y = (int)MathF.Round(viewportHeight * 0.5f - min * 0.5f);
        
        gpu.Renderbuffer.BindToWindow();
        gpu.Renderbuffer.ClearColorBuffers(0, 0, 0, 0);
        gpu.Renderbuffer.Blit(fb, x, y, min + x, min + y);
        
        gpu.ReleaseRenderbuffer(fb);
    }

    protected override void OnStop()
    {
    }
}