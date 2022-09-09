using System.Numerics;
using EasyGameFramework.Api;

namespace SimplePlatformer;

public class Player
{
    public ButtonInput JumpInput { get; }
    public AxisInput MovementInput { get; }
    
    private ILogger Logger { get; }
    public Vector2 Position { get; set; }

    public Player(ILogger logger)
    {
        Logger = logger;

        JumpInput = new ButtonInput();
        MovementInput = new AxisInput();

        JumpInput.Pressed += Jump;
    }
    
    public void Jump()
    {
        Logger.Trace("Jump!");
    }

    public void Update(float dt)
    {
        Position += new Vector2(MovementInput.Value * dt * 10f, 0f);
    }
}