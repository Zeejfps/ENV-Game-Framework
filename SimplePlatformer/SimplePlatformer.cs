﻿using System.Numerics;
using EasyGameFramework.Api;
using EasyGameFramework.Api.Cameras;
using EasyGameFramework.Api.InputDevices;
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
    
    public SimplePlatformer(IEventLoop eventLoop, ILogger logger, IInputSystem inputSystem, IGpu gpu) : base(eventLoop, logger)
    {
        var maxPlayerCount = 1;

        Gpu = gpu;
        
        Camera = new OrthographicCamera(100, 100, 0.1f, 100f);
        
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
        Camera.SetSize(50f, 50f / aspectRatio);
        CloseAppInput.Pressed += Stop;
        SpriteRenderer.LoadResources();
    }

    protected override void OnUpdate()
    {
        foreach (var player in Players)
        {
            player.Update(Clock.DeltaTime);
        }
    }

    protected override void OnRender()
    {
        var renderbuffer = Gpu.Renderbuffer;
        renderbuffer.ClearColorBuffers(0.2f, 0.2f, 0.2f, 1);
        
        SpriteRenderer.NewBatch();

        foreach (var player in Players)
        {
            SpriteRenderer.DrawSprite(player.Position, new Vector3(1f, 0f, 1f));
        }
        
        SpriteRenderer.RenderBatch(Camera);
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
}