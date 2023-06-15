using System.Numerics;
using EasyGameFramework.Api;
using EasyGameFramework.Api.Physics;

namespace Pong;

public class Ball : IBody
{
    public Ball(ILogger logger)
    {
        Logger = logger;
    }

    public Vector2 PrevPosition { get; set; }
    public Vector2 Position { get; set; }
    public Rect Bounds { get; set; }
    public Vector2 Velocity { get; set; } = new(20, 20);

    private ILogger Logger { get; }
    
    public void Update(float dt)
    {
        //PrevPosition = Position;
        
        var newPosition = Position + Velocity * dt;
        if (newPosition.X <= Bounds.Left + 0.5f)
        {
            Velocity = Velocity with { X = -Velocity.X };
            newPosition.X = Bounds.Left + 0.5f;
        }
        else if (newPosition.X >= Bounds.Right - 0.5f)
        {
            Velocity = Velocity with { X = -Velocity.X };
            newPosition.X = Bounds.Right - 0.5f;
        }
        
        if (newPosition.Y >= Bounds.Top)
            Velocity = Velocity with { Y = -Velocity.Y };
        else if (newPosition.Y <= Bounds.Bottom)
            Velocity = Velocity with { Y = -Velocity.Y };
        
        //Position = newPosition;
        //Logger.Trace(CurrPosition);
    }
}