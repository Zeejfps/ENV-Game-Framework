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
    private Paddle Paddle { get; }
    
    public Ball(IClock clock, Rectangle arena, Paddle paddle)
    {
        Clock = clock;
        Arena = arena;
        Paddle = paddle;
        Position = new Vector2(0, 0);
        Velocity = new Vector2(MaxSpeed, MaxSpeed);
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
        CheckAndResolveArenaCollisions(bounds);
        CheckAndResolvePaddleCollisions(bounds);
    }

    // NOTE(Zee): This will fail if the ball is travelling faster. (Fast enough to phase through the paddle)
    private void CheckAndResolvePaddleCollisions(Rectangle ballBounds)
    {
        var paddle = Paddle;
        var paddleBounds = paddle.CalculateBoundsRectangle();
        var ballIntersectsPaddle = ballBounds.Intersects(paddleBounds);
        if (!ballIntersectsPaddle)
            return;

        // TODO: I need to figure out if it should pop UP / DOWN or LEFT / RIGHT
        if (ballBounds.Left < paddleBounds.Left && ballBounds.Right > paddleBounds.Left)
        {
            var dx = paddleBounds.Left - ballBounds.Right;
            MoveX(-dx);
            ReflectVelocityX();
        }
        else if (ballBounds.Right > paddleBounds.Right && ballBounds.Left < paddleBounds.Right)
        {
            var dx = paddleBounds.Right - ballBounds.Left;
            MoveX(dx);
            ReflectVelocityX();
        }

        if (ballBounds.Top < paddleBounds.Top && ballBounds.Bottom > paddleBounds.Top)
        {
            var dy = paddleBounds.Top - ballBounds.Bottom;
            MoveY(dy);
            ReflectVelocityY();
        }
        else if(ballBounds.Bottom > paddleBounds.Bottom && ballBounds.Top < paddleBounds.Bottom)
        {
            var dy = ballBounds.Bottom - paddleBounds.Bottom; 
            MoveY(dy);
            ReflectVelocityY();
        }
    }

    private void MoveX(float dx)
    {
        Position += Vector2.UnitX * dx;
    }
    
    private void MoveY(float dx)
    {
        Position += Vector2.UnitX * dx;
    }

    private void ReflectVelocityX()
    {
        Velocity = Velocity with { X = Velocity.X * -1f };
    }

    private void ReflectVelocityY()
    {
        Velocity = Velocity with { Y = Velocity.Y * -1f };
    }

    private void CheckAndResolveArenaCollisions(Rectangle bounds)
    {
        if (bounds.Left < Arena.Left)
        {
            var dx = bounds.Left - Arena.Left;
            Position -= Vector2.UnitX * dx;
            ReflectVelocityX();
        }
        else if (bounds.Right > Arena.Right)
        {
            var dx = bounds.Right - Arena.Right;
            Position -= Vector2.UnitX * dx;
            ReflectVelocityX();
        }
        
        if (bounds.Top < Arena.Top)
        {
            var dx = bounds.Top - Arena.Top;
            Position -= Vector2.UnitY * dx;
            ReflectVelocityY();
        }
        else if (bounds.Bottom > Arena.Bottom)
        {
            var dx = bounds.Bottom - Arena.Bottom;
            Position -= Vector2.UnitY * dx;
            ReflectVelocityY();
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