using System.Numerics;
using EasyGameFramework.Api;
using EasyGameFramework.Api.Cameras;
using EasyGameFramework.Api.Enums;
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
    
    private IGpuTextureHandle? PlayerSpriteSheetTexture { get; set; }
    private SpriteSheet RunAnimationSpriteSheet { get; }
    
    private Animation Animation { get; }

    public SimplePlatformer(
        IEventLoop eventLoop,
        ILogger logger,
        IInputSystem inputSystem,
        IGpu gpu) : base(eventLoop, logger)
    {
        var maxPlayerCount = 1;

        Gpu = gpu;
        InputSystem = inputSystem;
        
        Camera = new OrthographicCamera(100, 100, 0.1f, 100f);
        Camera.Transform.WorldPosition += Vector3.UnitY * 5;
        
        SpriteRenderer = new SpriteRenderer(gpu);
        CloseAppInput = new ButtonInput();
        Players = new Player[maxPlayerCount];
        Controllers = new Controller[maxPlayerCount];
        
        PlayerSpriteSheetTexture = Gpu.Texture.Load("Assets/PlayerSpriteSheet", TextureFilterKind.Nearest);

        RunAnimationSpriteSheet = SpriteSheet.Create(new[]
        {

            new Sprite
            {
                Size = new Vector2(16, 16),
                Offset = new Vector2(16, 16 * 2),
                Pivot = new Vector2(0f, 0.5f),
                Texture = PlayerSpriteSheetTexture,
            },

            new Sprite
            {
                Size = new Vector2(16, 16),
                Offset = new Vector2(16 * 2, 16 * 2),
                Pivot = new Vector2(0f, 0.5f),
                Texture = PlayerSpriteSheetTexture,
            },

            new Sprite
            {
                Size = new Vector2(16, 16),
                Offset = new Vector2(16 * 3, 16 * 2),
                Pivot = new Vector2(0f, 0.5f),
                Texture = PlayerSpriteSheetTexture,
            },

            new Sprite
            {
                Size = new Vector2(16, 16),
                Offset = new Vector2(16 * 4, 16 * 2),
                Pivot = new Vector2(0f, 0.5f),
                Texture = PlayerSpriteSheetTexture,
            },

            new Sprite
            {
                Size = new Vector2(16, 16),
                Offset = new Vector2(16 * 5, 16 * 2),
                Pivot = new Vector2(0f, 0.5f),
                Texture = PlayerSpriteSheetTexture,
            },

            new Sprite
            {
                Size = new Vector2(16, 16),
                Offset = new Vector2(16 * 6, 16 * 2),
                Pivot = new Vector2(0f, 0.5f),
                Texture = PlayerSpriteSheetTexture,
            },
        });
        
        Animation = new Animation(Clock)
        {
            FrameCount = RunAnimationSpriteSheet.SpriteCount,
            FrameTime = 1f / 15f
        };

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

        Animation.Play();
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

    private int fps;
    private double dt;
    protected override void OnUpdate()
    {
        Animation.PlaybackSpeed = MathF.Abs(Players[0].Velocity.X);

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
        
        SpriteRenderer.NewBatch();
        var lerpFactor = Clock.FrameLerpFactor;
        foreach (var player in Players)
        {
            var position = Vector2.Lerp(player.PrevPosition, player.CurrPosition, lerpFactor);
            var sprite = RunAnimationSpriteSheet[Animation.FrameIndex];
            sprite.FlipX = player.IsMovingLeft;
            SpriteRenderer.DrawSprite(position, sprite);
        }
        SpriteRenderer.RenderBatch(Camera);
        
        fps++;
        dt += Clock.FrameDeltaTime;
        if (dt >= 1f)
        {
            Logger.Trace($"FPS: {fps}");
            fps = 0;
            dt = 0f;
        }
    }
}