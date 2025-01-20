using System.Numerics;

namespace Bricks;

public sealed class Ball
{
    // NOTE(Zee): Assuming this is the center position of the ball
    public Vector2 Position { get; set; }
    public Vector2 Velocity { get; set; }
    public const float MaxSpeed = 300;
    
    public const int Width = 25; 
    public const int Height = 25; 
    
    private IClock Clock { get; }
    private Rectangle Arena { get; }
    
    public Ball(IClock clock, Rectangle arena)
    {
        Clock = clock;
        Arena = arena;
        Position = new Vector2(0, 0);
        Velocity = new Vector2(MaxSpeed, MaxSpeed);
        Console.WriteLine(Velocity);
    }

    public void Update()
    {
        UpdatePosition();
        CheckAndResolveCollisions();
    }

    private void UpdatePosition()
    {
        Position += Velocity * Clock.DeltaTimeSeconds;
    }

    private void CheckAndResolveCollisions()
    {
        var bounds = CalculateBoundsRectangle();
        if (bounds.Left < Arena.Left)
        {
            var dx = bounds.Left - Arena.Left;
            Position -= Vector2.UnitX * dx;
            Velocity = Velocity with { X = Velocity.X * -1f };
        }
        else if (bounds.Right > Arena.Right)
        {
            var dx = bounds.Right - Arena.Right;
            Position -= Vector2.UnitX * dx;
            Velocity = Velocity with { X = Velocity.X * -1f };
        }
        
        if (bounds.Top < Arena.Top)
        {
            var dx = bounds.Top - Arena.Top;
            Position -= Vector2.UnitY * dx;
            Velocity = Velocity with { Y = Velocity.Y * -1f };
        }
        else if (bounds.Bottom > Arena.Bottom)
        {
            var dx = bounds.Bottom - Arena.Bottom;
            Position -= Vector2.UnitY * dx;
            Velocity = Velocity with { Y = Velocity.Y * -1f };
        }
    }

    public Rectangle CalculateBoundsRectangle()
    {
        var halfWidth = Width * 0.5f;
        var halfHeight = Height * 0.5f;
        var left = Position.X - halfWidth;
        var top = Position.Y - halfHeight;
        return Rectangle.LeftTopWidthHeight(left, top, Width, Height);
    }
}