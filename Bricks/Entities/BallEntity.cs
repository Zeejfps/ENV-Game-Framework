using System.Numerics;
using Bricks.Archetypes;
using Bricks.Repos;

namespace Bricks.Entities;

public sealed class BallEntity : IBall, IDynamicEntity
{
    // NOTE(Zee): Assuming this is the center position of the ball
    public Vector2 Position { get; set; }
    public Vector2 Velocity { get; set; }
    public const float MaxSpeed = 300;
    
    public const int Width = 25; 
    public const int Height = 25; 
    
    private IClock Clock => World.Clock;
    private Rectangle Arena { get; }
    private PaddleEntity Paddle { get; }
    private BricksRepo Bricks { get; }
    private World World { get; }

    public BallEntity(Rectangle arena, World world)
    {
        Arena = arena;
        World = world;
        Paddle = world.Paddle;
        Bricks = world.Bricks;
        Position = new Vector2(arena.Width * 0.5f, arena.Height * 0.5f);
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
        var bounds = GetAABB();
        CheckAndResolveArenaCollisions(bounds);
        CheckAndResolvePaddleCollisions(bounds);
        CheckAndResolveBrickCollisions(bounds);
    }

    // NOTE(Zee): This will fail if the ball is travelling faster. (Fast enough to phase through the paddle)
    private void CheckAndResolvePaddleCollisions(Rectangle ballBounds)
    {
        var paddle = Paddle;
        var paddleBounds = paddle.GetAABB();
        CheckAndResolveCollision(ballBounds, paddleBounds);
    }

    private void CheckAndResolveBrickCollisions(Rectangle ballBounds)
    {
        var bricks = Bricks.GetAll();
        foreach (var brick in bricks)
        {
            var brickBounds = brick.GetAABB();
            var collided = CheckAndResolveCollision(ballBounds, brickBounds);
            if (collided)
            {
                brick.TakeDamage();
            }
        }
    }

    private bool CheckAndResolveCollision(Rectangle ballBounds, Rectangle otherBounds)
    {
        var ballIntersectsPaddle = ballBounds.Intersects(otherBounds);
        if (!ballIntersectsPaddle)
            return false;

        // TODO: I need to figure out if it should pop UP / DOWN or LEFT / RIGHT
        var dx = 0f;
        if (ballBounds.Left < otherBounds.Left && ballBounds.Right > otherBounds.Left)
        {
            dx = otherBounds.Left - ballBounds.Right;
        }
        else if (ballBounds.Right > otherBounds.Right && ballBounds.Left < otherBounds.Right)
        {
            dx = otherBounds.Right - ballBounds.Left;
        }

        var dy = 0f;
        if (ballBounds.Top < otherBounds.Top && ballBounds.Bottom > otherBounds.Top)
        {
            dy = otherBounds.Top - ballBounds.Bottom;
        }
        else if(ballBounds.Bottom > otherBounds.Bottom && ballBounds.Top < otherBounds.Bottom)
        {
            dy = otherBounds.Bottom - ballBounds.Top; 
        }

        var ady = MathF.Abs(dy);
        var adx = MathF.Abs(dx);
        if (adx > 0 && ady > 0)
        {
            if (ady < adx)
            {
                ReflectVelocityY();
                MoveY(dy);
                MoveYWithVelocity();
            }
            else
            {
                ReflectVelocityX();
                MoveX(dx);
                MoveXWithVelocity();
            }
        }
        else if (ady > 0)
        {
            ReflectVelocityY();
            MoveY(dy);
            MoveYWithVelocity();
        }
        else if (adx > 0)
        {
            ReflectVelocityX();
            MoveX(dx);
            MoveXWithVelocity();
        }

        return true;
    }

    private void MoveXWithVelocity()
    {
        MoveX(Velocity.X * Clock.DeltaTimeSeconds);
    }

    private void MoveYWithVelocity()
    {
        MoveY(Velocity.Y * Clock.DeltaTimeSeconds);
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

    public Rectangle GetAABB()
    {
        var halfWidth = Width * 0.5f;
        var halfHeight = Height * 0.5f;
        var left = Position.X - halfWidth;
        var top = Position.Y - halfHeight;
        return Rectangle.LeftTopWidthHeight(left, top, Width, Height);
    }

    public void Spawn()
    {
        World.Balls.Add(this);
        World.DynamicEntities.Add(this);
    }

    public void Despawn()
    {
        World.Balls.Remove(this);
        World.DynamicEntities.Remove(this);
    }
}