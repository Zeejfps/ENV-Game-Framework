using System.Numerics;
using EasyGameFramework.Api;
using EasyGameFramework.Api.Cameras;
using EasyGameFramework.Api.InputDevices;
using EasyGameFramework.Api.Physics;
using Pong.Physics;

namespace Pong;

public sealed class PongGame : Game
{
    private IInputSystem InputSystem => Window.Input;
    private SpriteRenderer SpriteRenderer { get; }
    private OrthographicCamera Camera { get; }
    private Sprite PaddleSprite { get; set; }
    //private Sprite BallSprite { get; set; }

    private Physics2D Physics2D = new Physics2D();
    private IViewport Viewport { get; }
    private IViewport Viewport2 { get; }

    private Rect LevelBounds = new()
    {
        BottomLeft = new Vector2(-50, -50),
        Width = 100,
        Height = 100,
    };
    
    private Paddle BottomPaddle { get; }
    private Paddle TopPaddle { get; }
    private Ball Ball { get; }

    private BallPaddleCollisionSystem BallPaddleCollisionSystem { get; } = new BallPaddleCollisionSystem();

    public PongGame(IContext context) : base(context)
    {
        Camera = OrthographicCamera.Create(LevelBounds.Width, LevelBounds.Height, 0.01f, 10f);
        Viewport = new Viewport
        {
            Left = 0.5f,
            AspectRatio = Camera.AspectRatio
        };
        Viewport2 = new Viewport
        {
            Right = 0.5f,
            AspectRatio = Camera.AspectRatio
        };

        SpriteRenderer = new SpriteRenderer(Gpu);

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

        Ball = new Ball(Logger)
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
        window.SetScreenSize(640, 640);
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
        var mouseScreenPoint = new Vector2(InputSystem.Mouse.ScreenX, InputSystem.Mouse.ScreenY);
        var viewports = new IViewport[] { Viewport, Viewport2 };
        var mouseViewportPoint = Vector2.Zero;
        for (var i = 0; i < viewports.Length; i++)
        {
            mouseViewportPoint = Window.ScreenToViewportPoint(mouseScreenPoint, viewports[i]);
            if (mouseViewportPoint.X < 0f || mouseViewportPoint.X > 1f || 
                mouseViewportPoint.Y < 0f || mouseViewportPoint.Y > 1f) 
                continue;

            break;
        }
        
        var mouseWorldPoint = Camera.ViewportToWorldPoint(mouseViewportPoint);

        var rayOrigin = new Vector2(-40, 40);
        var rayDirection = mouseWorldPoint - rayOrigin;
        var ray = new Ray2D
        {
            Origin = rayOrigin,
            Direction = rayDirection
        };

        var rect = new Rect
        {
            BottomLeft = new Vector2(-20, -20),
            Width = 40,
            Height = 40
        };
        
        m_IsHit = Physics2D.TryRaycastRect(ray, rect, out var result);
        if (m_IsHit)
        {
            //Logger.Trace($"Hit: {result.HitPoint}");
        }
        
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

    private bool m_IsHit;
    
    protected override void OnRender()
    {
        var camera = Camera;
        var gpu = Gpu;
        
        var frameLerpFactor = Time.FrameLerpFactor;
        SpriteRenderer.NewBatch();
        {
            var paddle1Pos = Vector2.Lerp(BottomPaddle.PrevPosition, BottomPaddle.CurrPosition, frameLerpFactor);
            SpriteRenderer.DrawSprite(paddle1Pos,  new Vector2(10f, 1f), PaddleSprite, Vector3.One);

            var paddle2Pos = Vector2.Lerp(TopPaddle.PrevPosition, TopPaddle.CurrPosition, frameLerpFactor);
            SpriteRenderer.DrawSprite(paddle2Pos,  new Vector2(10f, 1f), PaddleSprite, Vector3.One);

            var ballPosition = Vector2.Lerp(Ball.PrevPosition, Ball.CurrPosition, frameLerpFactor);
            SpriteRenderer.DrawSprite(ballPosition,  new Vector2(1f, 1f), PaddleSprite, new Vector3(0.5f, 0.7f, 0.1f));
            
            SpriteRenderer.DrawSprite(new Vector2(0, 0),  
                new Vector2(20f, 20f),
                PaddleSprite, 
                m_IsHit ? new Vector3(1f, 0.7f, 0.1f) : new Vector3(0, 0, 0.2f));
        }

        var fb = gpu.CreateRenderbuffer(1, false, 200, (int)(200 * camera.AspectRatio));
        gpu.Renderbuffer.Bind(fb);
        gpu.Renderbuffer.ClearColorBuffers(0f, 0.3f, 0f, 1f);
        SpriteRenderer.RenderBatch(camera);
        
        gpu.Renderbuffer.BindToWindow();
        gpu.Renderbuffer.ClearColorBuffers(0, 0, 0, 0);
        gpu.Renderbuffer.Blit(fb, Viewport);
        gpu.Renderbuffer.Blit(fb, Viewport2);
        
        gpu.ReleaseRenderbuffer(fb);
    }

    protected override void OnStop()
    {
    }
}