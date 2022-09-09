using System.Numerics;
using EasyGameFramework.Api;

namespace SimplePlatformer;

public class Player
{
    public ButtonInput JumpInput { get; }
    public AxisInput MovementInput { get; }
    public ButtonInput ResetInput { get; } = new();
    
    private ILogger Logger { get; }
    public Vector2 Position { get; set; }
    
    private Vector2 Acceleration { get; set; }
    
    public Vector2 Velocity { get; set; }

    public Player(ILogger logger)
    {
        Logger = logger;

        JumpInput = new ButtonInput();
        MovementInput = new AxisInput();

        JumpInput.Pressed += Jump;
        ResetInput.Pressed += ResetSpeedInput_OnPressed;
    }

    private void ResetSpeedInput_OnPressed()
    {
        Velocity = Vector2.Zero;
    }

    public void AddForce(Vector2 force)
    {
        Acceleration += force;
    }
    
    private void Jump()
    {
        Velocity = Velocity with { Y = 0f };
        AddForce(Vector2.UnitY * 30f);
        Logger.Trace("Jump");
    }

    public void Update(float dt)
    {
        AddForce(Vector2.UnitX * MovementInput.Value);
        
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
        
        if (Position.X < -18f || Position.X > 16.5f)
            Velocity = Velocity with { X = -Velocity.X };
        
        Acceleration = Vector2.Zero;
    }
}