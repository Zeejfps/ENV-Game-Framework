using System.Numerics;
using EasyGameFramework.Api;

namespace SimplePlatformer;

public class Player
{
    public ButtonInput JumpInput { get; }
    public AxisInput MovementInput { get; }
    
    private ILogger Logger { get; }
    public Vector2 Position { get; set; }
    
    private Vector2 Acceleration { get; set; }
    
    private Vector2 Velocity { get; set; }

    public Player(ILogger logger)
    {
        Logger = logger;

        JumpInput = new ButtonInput();
        MovementInput = new AxisInput();

        JumpInput.Pressed += Jump;
    }

    public void AddForce(Vector2 force)
    {
        Acceleration += force;
    }
    
    private void Jump()
    {
        AddForce(Vector2.UnitY * 12f);
        Logger.Trace("Jump");
    }

    public void Update(float dt)
    {
        if (Position.Y > 0f)
        {
            AddForce(Vector2.UnitY * -0.98f);
        }
        else
        {
            Velocity = Velocity with { Y = 0f };
            Position = Position with { Y = 0f };
        }
        
        Velocity += Acceleration;
        Position += Velocity * dt;
        Acceleration = Vector2.Zero;
    }
}