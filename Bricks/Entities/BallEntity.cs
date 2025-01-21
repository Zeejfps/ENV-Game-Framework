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
    private AABB Arena { get; }
    private PaddleEntity Paddle { get; }
    private BricksRepo Bricks { get; }
    private World World { get; }
    
    public BallEntity(AABB arena, World world)
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
        if (CheckAndResolveBrickCollisions() ||
            CheckAndResolvePaddleCollisions() ||
            CheckAndResolveArenaCollisions()
        ) {
            return;
        }

        Position += Velocity * Clock.DeltaTimeSeconds;
        CheckAndResolveIntersection();
    }

    private bool CheckAndResolvePaddleCollisions()
    {
        var paddle = Paddle;
        var movement = Velocity;
        var maxDistance = (Velocity * Clock.DeltaTimeSeconds).Length();
        var paddleBounds = paddle.GetAABB();
        var ballAABB = GetAABB();
        if (ballAABB.TryCast(movement, paddleBounds, out var result) && result.Distance <= maxDistance)
        {
            ReflectVelocity(result.Normal);
            Position += Vector2.Normalize(Velocity) * result.Distance;
            return true;
        }

        return false;
    }

    private bool CheckAndResolveBrickCollisions()
    {
        var movement = Velocity * Clock.DeltaTimeSeconds;
        var moveDir = Velocity;
        var moveDist = movement.Length();
        var bricks = Bricks.GetAll();
        var ballAABB = GetAABB();
        var positionUpdated = false;
        foreach (var brick in bricks)
        {
            var brickBounds = brick.GetAABB();
            if (ballAABB.TryCast(moveDir, brickBounds, out var result) && result.Distance < moveDist)
            {
                brick.TakeDamage();
                ReflectVelocity(result.Normal);
                Position += Vector2.Normalize(moveDir) * result.Distance;
                return true;
            }
        }
        return positionUpdated;
    }

    private void ReflectVelocity(Vector2 normal)
    {
        if (normal == Vector2.UnitY || normal == -Vector2.UnitY)
        {
            ReflectVelocityY();
        }
        else if (normal == Vector2.UnitX || normal == -Vector2.UnitX)
        {
            ReflectVelocityX();
        }
    }

    private void CheckAndResolveIntersection()
    {
        var ballBounds = GetAABB();
        var otherBounds = Paddle.GetAABB();
        var intersectionExists = ballBounds.Intersects(otherBounds);
        if (!intersectionExists)
            return;
        
        // TODO: I need to figure out if it should pop UP / DOWN or LEFT / RIGHT
        var dx = 0f;
        if (ballBounds.Left.IsLeft(otherBounds.Left) && ballBounds.Right.IsRight(otherBounds.Left))
        {
            dx = otherBounds.Left - ballBounds.Right;
        }
        else if (ballBounds.Right.IsRight(otherBounds.Right) && ballBounds.Left.IsLeft(otherBounds.Right))
        {
            dx = otherBounds.Right - ballBounds.Left;
        }
        
        var dy = 0f;
        if (ballBounds.Top.IsAbove(otherBounds.Top) && ballBounds.Bottom.IsBelow(otherBounds.Top))
        {
            dy = otherBounds.Top - ballBounds.Bottom;
        }
        else if(ballBounds.Bottom.IsBelow(otherBounds.Bottom) && ballBounds.Top.IsAbove(otherBounds.Bottom))
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

    private bool CheckAndResolveArenaCollisions()
    {
        var positionAdjusted = false;
        var bounds = GetAABB();
        if (bounds.Left.IsLeft(Arena.Left))
        {
            var dx = bounds.Left - Arena.Left;
            Position -= Vector2.UnitX * dx;
            ReflectVelocityX();
            positionAdjusted = true;
        }
        else if (bounds.Right.IsRight(Arena.Right))
        {
            var dx = bounds.Right - Arena.Right;
            Position -= Vector2.UnitX * dx;
            ReflectVelocityX();
            positionAdjusted = true;
        }
        
        if (bounds.Top.IsAbove(Arena.Top))
        {
            var dx = bounds.Top - Arena.Top;
            Position -= Vector2.UnitY * dx;
            ReflectVelocityY();
            positionAdjusted = true;
        }
        else if (bounds.Bottom.IsBelow(Arena.Bottom))
        {
            var dx = bounds.Bottom - Arena.Bottom;
            Position -= Vector2.UnitY * dx;
            ReflectVelocityY();
            positionAdjusted = true;
        }

        return positionAdjusted;
    }

    public AABB GetAABB()
    {
        var halfWidth = Width * 0.5f;
        var halfHeight = Height * 0.5f;
        var left = Position.X - halfWidth;
        var top = Position.Y - halfHeight;
        return AABB.FromLeftTopWidthHeight(left, top, Width, Height);
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