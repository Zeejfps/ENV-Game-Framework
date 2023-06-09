using System.Numerics;
using EasyGameFramework.Api;
using EasyGameFramework.Api.Cameras;
using EasyGameFramework.Api.InputDevices;
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
    
    public PongGame(IWindow window, IEventLoop eventLoop, ILogger logger) : base(eventLoop, logger)
    {
        Window = window;
        SpriteRenderer = new SpriteRenderer(Gpu);
        Camera = new OrthographicCamera(100, 0.1f, 100f);
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

    private Paddle Paddle1 { get; } = new()
    {
        CurrPosition = new Vector2(0, -40f),
        PrevPosition = new Vector2(0f, -40f)
    };
    
    private Paddle Paddle2 { get; } = new()
    {
        CurrPosition = new Vector2(0, 40f),
        PrevPosition = new Vector2(0f, 40f)
    };

    protected override void OnUpdate()
    {
        var keyboard = InputSystem.Keyboard;
        if (keyboard.IsKeyPressed(KeyboardKey.Escape))
            Window.Close();

        var paddleSpeed = 30f;
        
        Paddle1.PrevPosition = Paddle1.CurrPosition;
        if (keyboard.IsKeyPressed(KeyboardKey.A))
            Paddle1.CurrPosition -= Vector2.UnitX * Clock.UpdateDeltaTime * paddleSpeed;
        else if (keyboard.IsKeyPressed(KeyboardKey.D))
            Paddle1.CurrPosition += Vector2.UnitX * Clock.UpdateDeltaTime * paddleSpeed;

        Paddle2.PrevPosition = Paddle2.CurrPosition;
        if (keyboard.IsKeyPressed(KeyboardKey.LeftArrow))
            Paddle2.CurrPosition -= Vector2.UnitX * Clock.UpdateDeltaTime * paddleSpeed;
        else if (keyboard.IsKeyPressed(KeyboardKey.RightArrow))
            Paddle2.CurrPosition += Vector2.UnitX * Clock.UpdateDeltaTime * paddleSpeed;
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
        }
        SpriteRenderer.RenderBatch(camera);
    }

    protected override void OnStop()
    {
    }
}