using System.Numerics;
using EasyGameFramework.Api.Physics;

namespace Pong;

public class Ball
{
    public Vector2 PrevPosition { get; set; }
    public Vector2 CurrPosition { get; set; }
    public Rect Bounds { get; set; }
    public Vector2 Velocity { get; set; } = new(20, 20);
    
    public void Update(float dt)
    {
        PrevPosition = CurrPosition;
        
        var newPosition = CurrPosition + Velocity * dt;
        if (newPosition.X <= Bounds.Left)
            Velocity = Velocity with { X = -Velocity.X };
        else if (newPosition.X >= Bounds.Right)
            Velocity = Velocity with { X = -Velocity.X };
        
        if (newPosition.Y >= Bounds.Top)
            Velocity = Velocity with { Y = -Velocity.Y };
        else if (newPosition.Y <= Bounds.Bottom)
            Velocity = Velocity with { Y = -Velocity.Y };
        
        CurrPosition = newPosition;
    }
}