using System.Numerics;
using EasyGameFramework.Api;
using EasyGameFramework.Api.Cameras;
using EasyGameFramework.Api.InputDevices;
using EasyGameFramework.Api.Physics;
using EasyGameFramework.Api.Rendering;

namespace Pong;

public sealed class PongGame : Game
{
    private IWindow Window { get; }
    private IGpu Gpu => Window.Gpu;
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
    
    private Paddle Paddle1 { get; }
    private Paddle Paddle2 { get; }
    private Ball Ball { get; }
    
    public PongGame(IWindow window, IEventLoop eventLoop, ILogger logger) : base(eventLoop, logger)
    {
        Window = window;
        SpriteRenderer = new SpriteRenderer(Gpu);
        Camera = new OrthographicCamera(100, 0.1f, 100f);
        
        Paddle1 = new()
        {
            CurrPosition = new Vector2(0, -40f),
            PrevPosition = new Vector2(0f, -40f),
            Bounds = LevelBounds
        };
    
        Paddle2 = new()
        {
            CurrPosition = new Vector2(0, 40f),
            PrevPosition = new Vector2(0f, 40f),
            Bounds = LevelBounds
        };

        Ball = new Ball
        {
            Velocity = new Vector2(10, 30),
            Bounds = LevelBounds
        };
    }

    protected override void OnStart()
    {
        var texture = Gpu.Texture.Load("Assets/white");
        
        PaddleSprite = new Sprite
        {
            Color = new Vector3(1f, 1f, 1f),
            Size = new Vector2(32f, 32f),
            Texture = texture
        };
        SpriteRenderer.LoadResources();
    }

    protected override void OnUpdate()
    {
        var keyboard = InputSystem.Keyboard;
        if (keyboard.IsKeyPressed(KeyboardKey.Escape))
            Window.Close();

        Ball.Update(Clock.UpdateDeltaTime);
        
        var paddleSpeed = 30f;
        var paddlePositionDelta = Clock.UpdateDeltaTime * paddleSpeed;
        
        if (keyboard.IsKeyPressed(KeyboardKey.A))
            Paddle1.MoveLeft(paddlePositionDelta);
        else if (keyboard.IsKeyPressed(KeyboardKey.D))
            Paddle1.MoveRight(paddlePositionDelta);
        
        if (keyboard.IsKeyPressed(KeyboardKey.LeftArrow))
            Paddle2.MoveLeft(paddlePositionDelta);
        else if (keyboard.IsKeyPressed(KeyboardKey.RightArrow))
            Paddle2.MoveRight(paddlePositionDelta);
    }

    protected override void OnRender()
    {
        var camera = Camera;
        var gpu = Gpu;
        gpu.Renderbuffer.ClearColorBuffers(0f, 0f, 0f, 1f);
        
        SpriteRenderer.NewBatch();
        {
            var paddle1Pos = Vector2.Lerp(Paddle1.PrevPosition, Paddle1.CurrPosition, Clock.FrameLerpFactor);
            SpriteRenderer.DrawSprite(paddle1Pos,  new Vector2(10f, 1f), PaddleSprite);

            var paddle2Pos = Vector2.Lerp(Paddle2.PrevPosition, Paddle2.CurrPosition, Clock.FrameLerpFactor);
            SpriteRenderer.DrawSprite(paddle2Pos,  new Vector2(10f, 1f), PaddleSprite);
            
            var ballPosition = Vector2.Lerp(Ball.PrevPosition, Ball.CurrPosition, Clock.FrameLerpFactor);
            SpriteRenderer.DrawSprite(ballPosition,  new Vector2(1f, 1f), PaddleSprite);
        }
        SpriteRenderer.RenderBatch(camera);
    }

    protected override void OnStop()
    {
    }
}