using System.Numerics;
using EasyGameFramework.Api;
using EasyGameFramework.Api.Cameras;
using EasyGameFramework.Api.InputDevices;
using EasyGameFramework.Api.Physics;
using EasyGameFramework.Api.Rendering;

namespace Pong;

public sealed class PongGame : Game
{
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
    
    private Paddle BottomPaddle { get; }
    private Paddle TopPaddle { get; }
    private Ball Ball { get; }

    private BallPaddleCollisionSystem BallPaddleCollisionSystem { get; } = new BallPaddleCollisionSystem();

    public PongGame(IWindow window, ILogger logger) : base(window, logger)
    {
        SpriteRenderer = new SpriteRenderer(Gpu);
        Camera = new OrthographicCamera(100, 0.1f, 100f);
        
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
        gpu.Renderbuffer.ClearColorBuffers(0f, 0f, 0f, 1f);
        
        SpriteRenderer.NewBatch();
        {
            var paddle1Pos = Vector2.Lerp(BottomPaddle.PrevPosition, BottomPaddle.CurrPosition, Clock.FrameLerpFactor);
            SpriteRenderer.DrawSprite(paddle1Pos,  new Vector2(10f, 1f), PaddleSprite);

            var paddle2Pos = Vector2.Lerp(TopPaddle.PrevPosition, TopPaddle.CurrPosition, Clock.FrameLerpFactor);
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