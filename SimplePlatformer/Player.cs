using System.Numerics;
using EasyGameFramework.Api;

namespace SimplePlatformer;

public class Player
{
    public ButtonInput JumpInput { get; }
    public AxisInput MovementInput { get; }
    public ButtonInput ResetVelocityInput { get; } = new();
    
    private ILogger Logger { get; }
    public Vector2 PrevPosition { get; set; }
    public Vector2 CurrPosition { get; set; }
    public Vector2 Velocity { get; set; }
    
    private Vector2 Acceleration { get; set; }

    public Player(ILogger logger)
    {
        Logger = logger;

        JumpInput = new ButtonInput();
        MovementInput = new AxisInput();

        JumpInput.Pressed += Jump;
        ResetVelocityInput.Pressed += ResetSpeedInput_OnPressed;
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
        AddForce(Vector2.UnitY * 20f);
        Logger.Trace("Jump");
    }

    public void Update(float dt)
    {
        AddForce(Vector2.UnitX * MovementInput.Value * dt * 30f);
        
        if (CurrPosition.Y > 0f)
        {
            AddForce(Vector2.UnitY * -60.8f * dt);
        }
        else
        {
            Velocity = Velocity with { Y = 0f };
            CurrPosition = CurrPosition with { Y = 0f };

            if (Velocity.LengthSquared() > 0.1f)
            {
                var frictionDir = Vector2.Normalize(Velocity);
                AddForce(frictionDir * -0.45f);
            }
            else
            {
                Velocity = Vector2.Zero;
            }
        }
        
        Velocity += Acceleration;
        PrevPosition = CurrPosition;
        CurrPosition += Velocity * dt;

        //Logger.Trace(Velocity);
        
        if (CurrPosition.X < -17.3f/2f)
        {
            CurrPosition = CurrPosition with { X = -17.3f /2f};
            Velocity = Velocity with { X = -Velocity.X };
        }
        else if (CurrPosition.X > 17.3f/2f)
        {
            CurrPosition = CurrPosition with { X =  17.3f/2f };
            Velocity = Velocity with { X = -Velocity.X };
        }
        
        Acceleration = Vector2.Zero;
    }
}