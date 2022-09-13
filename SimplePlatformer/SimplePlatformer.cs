using System.Numerics;
using EasyGameFramework.Api;
using EasyGameFramework.Api.Cameras;
using EasyGameFramework.Api.InputDevices;
using EasyGameFramework.Api.Physics;
using EasyGameFramework.Api.Rendering;
using SampleGames;

namespace SimplePlatformer;

public class SimplePlatformer : Game
{
    private IGpu Gpu { get; }
    private Player[] Players { get; }
    private Controller[] Controllers { get; }
    private ButtonInput CloseAppInput { get; }
    private SpriteRenderer SpriteRenderer { get; }
    private OrthographicCamera Camera { get; }
    private IInputSystem InputSystem { get; }
    
    private IGpuTextureHandle? PlayerSpriteSheet { get; set; }
    private Sprite[] Animation { get; set; }
    
    private float FrameTime { get; set; }
    private int Index { get; set; }

    public SimplePlatformer(IEventLoop eventLoop, ILogger logger, IInputSystem inputSystem, IGpu gpu) : base(eventLoop, logger)
    {
        var maxPlayerCount = 2;

        Gpu = gpu;
        InputSystem = inputSystem;
        
        Camera = new OrthographicCamera(100, 100, 0.1f, 100f);
        Camera.Transform.WorldPosition += Vector3.UnitY * 5;
        
        SpriteRenderer = new SpriteRenderer(gpu);
        CloseAppInput = new ButtonInput();
        Players = new Player[maxPlayerCount];
        Controllers = new Controller[maxPlayerCount];
        
        for (var i = 0; i < maxPlayerCount; i++)
        {
            var player = new Player(logger);
            var controller = new Controller(inputSystem, Clock);

            Players[i] = player;
            Controllers[i] = controller;

            controller.Bind(player.JumpInput)
                .To(KeyboardKey.Space)
                .To(GamepadButton.South);

            controller.Bind(player.MovementInput)
                .To(KeyboardKey.A, -1f)
                .To(KeyboardKey.D, 1f)
                .To(GamepadAxis.LeftStickX, 0.08f);

            controller.Bind(player.ResetVelocityInput)
                .To(KeyboardKey.R)
                .To(GamepadButton.East);
            
            controller.Bind(CloseAppInput)
                .To(KeyboardKey.Escape)
                .To(GamepadButton.Back);
        }
    }

    protected override void OnStart()
    {
        for (var i = 0; i < Controllers.Length; i++)
        {
            var controller = Controllers[i];
            controller.Attach(i);
        }

        var aspectRatio = Gpu.Renderbuffer.Width / Gpu.Renderbuffer.Height;
        Camera.SetSize(10f * aspectRatio, 10);
        CloseAppInput.Pressed += Stop;
        SpriteRenderer.LoadResources();

        PlayerSpriteSheet = Gpu.Texture.Load("Assets/PlayerSpriteSheet");

        Animation = new[]
        {

            new Sprite
            {
                Size = new Vector2(16, 16),
                Offset = new Vector2(16, 16 * 2),
                Pivot = new Vector2(0f, 0.5f),
                SpriteSheet = PlayerSpriteSheet,
            },
            
            new Sprite
            {
                Size = new Vector2(16, 16),
                Offset = new Vector2(16 * 2, 16 * 2),
                Pivot = new Vector2(0f, 0.5f),
                SpriteSheet = PlayerSpriteSheet,
            },
            
            new Sprite
            {
                Size = new Vector2(16, 16),
                Offset = new Vector2(16 * 3, 16 * 2),
                Pivot = new Vector2(0f, 0.5f),
                SpriteSheet = PlayerSpriteSheet,
            },
            
            new Sprite
            {
                Size = new Vector2(16, 16),
                Offset = new Vector2(16 * 4, 16 * 2),
                Pivot = new Vector2(0f, 0.5f),
                SpriteSheet = PlayerSpriteSheet,
            },
            
            new Sprite
            {
                Size = new Vector2(16, 16),
                Offset = new Vector2(16 * 5, 16 * 2),
                Pivot = new Vector2(0f, 0.5f),
                SpriteSheet = PlayerSpriteSheet,
            },
            
            new Sprite
            {
                Size = new Vector2(16, 16),
                Offset = new Vector2(16 * 6, 16 * 2),
                Pivot = new Vector2(0f, 0.5f),
                SpriteSheet = PlayerSpriteSheet,
            },
        };
    }

    protected override void OnStop()
    {
        CloseAppInput.Pressed -= Stop;
        for (var i = 0; i < Controllers.Length; i++)
        {
            var controller = Controllers[i];
            controller.Detach();
        }
    }

    protected override void OnUpdate()
    {
        FrameTime += Clock.UpdateDeltaTime * MathF.Abs(Players[0].Velocity.X);
        if (FrameTime >= 0.15f)
        {
            FrameTime = 0f;

            Index++;
            if (Index >= Animation.Length)
            {
                Index = 0;
            }
        }

        foreach (var player in Players)
        {
            player.Update(Clock.DeltaTime);
        }
    }

    protected override void OnRender()
    {
        Gpu.EnableBlending = true;
        
        var renderbuffer = Gpu.Renderbuffer;
        renderbuffer.ClearColorBuffers(0.2f, 0.2f, 0.2f, 1);

        SpriteRenderer.NewBatch();

        var rect = new Rect
        {
            Position = new Vector2(0, 0),
            Width = 100,
            Height = 100,
        };

        if (rect.Contains(new Vector2(InputSystem.Mouse.ViewportX, InputSystem.Mouse.ViewportY)))
        {
            Logger.Trace("mouse in rect");
        }


        // SpriteRenderer.DrawSprite(rect.Position, new Sprite
        // {
        //     Color = new Vector3(1f, 0f, 1f),
        //     Size = new Vector2(rect.Width, rect.Height),
        // });
        //
        var lerpFactor = Clock.FrameLerpFactor;
        foreach (var player in Players)
        {
            var position = Vector2.Lerp(player.PrevPosition, player.CurrPosition, lerpFactor);
            ref var sprite = ref Animation[Index];

            if (sprite.FlipX && player.Velocity.X > 0.001f)
            {
                Logger.Trace(player.Velocity.X);
                sprite.FlipX = false;
            }
            else if (!sprite.FlipX && player.Velocity.X < -0.001f)
            {
                sprite.FlipX = true;
            }
            
            SpriteRenderer.DrawSprite(position, sprite);
        }
        
        SpriteRenderer.RenderBatch(Camera);
    }
}