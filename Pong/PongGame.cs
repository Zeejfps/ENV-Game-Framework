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
    private Paddle Paddle { get; }
    private SpriteRenderer SpriteRenderer { get; }
    private ICamera Camera { get; }
    
    private Sprite PaddleSprite { get; set; }
    
    public PongGame(IWindow window, IEventLoop eventLoop, ILogger logger) : base(eventLoop, logger)
    {
        Window = window;
        Paddle = new Paddle();
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

    private Vector2 PaddlePrevPos { get; set; } = new(0f, -40f);
    private Vector2 PaddlePos { get; set; }= new(0f, -40f);

    protected override void OnUpdate()
    {
        var keyboard = InputSystem.Keyboard;
        if (keyboard.IsKeyPressed(KeyboardKey.Escape))
            Window.Close();

        PaddlePrevPos = PaddlePos;
        if (keyboard.IsKeyPressed(KeyboardKey.A))
            PaddlePos -= Vector2.UnitX * Clock.UpdateDeltaTime * 30f;
        else if (keyboard.IsKeyPressed(KeyboardKey.D))
            PaddlePos += Vector2.UnitX * Clock.UpdateDeltaTime * 30f;
    }

    protected override void OnRender()
    {
        var camera = Camera;
        var gpu = Gpu;
        gpu.Renderbuffer.ClearColorBuffers(0f, 0f, 0f, 1f);
        
        SpriteRenderer.NewBatch();
        {
            var pos = Vector2.Lerp(PaddlePrevPos, PaddlePos, Clock.FrameLerpFactor);
            SpriteRenderer.DrawSprite(pos,  new Vector2(10f, 1f), PaddleSprite);
        }
        SpriteRenderer.RenderBatch(camera);
    }

    protected override void OnStop()
    {
    }
}