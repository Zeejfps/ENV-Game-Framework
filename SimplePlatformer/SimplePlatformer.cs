using EasyGameFramework.Api;
using EasyGameFramework.Api.InputDevices;

namespace SimplePlatformer;

public class SimplePlatformer : Game
{
    private Player[] Players { get; }
    private Controller[] Controllers { get; }
    
    public SimplePlatformer(IEventLoop eventLoop, ILogger logger, IInputSystem inputSystem) : base(eventLoop, logger)
    {
        var maxPlayerCount = 1;

        Players = new Player[maxPlayerCount];
        Controllers = new Controller[maxPlayerCount];
        for (var i = 0; i < maxPlayerCount; i++)
        {
            var player = new Player(logger);
            var controller = new Controller(inputSystem, Clock);

            Players[i] = player;
            Controllers[i] = controller;

            controller.Bind(player.Jump)
                .To(KeyboardKey.Space)
                .To(GamepadButton.South);

            controller.Bind(player.Move)
                .To(KeyboardKey.A, -1f)
                .To(KeyboardKey.D, 1f)
                .To(GamepadAxis.LeftStickX, 0.08f);
            
            controller.Bind(Stop)
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
    }

    protected override void OnUpdate()
    {
    }

    protected override void OnRender()
    {
    }

    protected override void OnStop()
    {
        for (var i = 0; i < Controllers.Length; i++)
        {
            var controller = Controllers[i];
            controller.Detach();
        }
    }
}