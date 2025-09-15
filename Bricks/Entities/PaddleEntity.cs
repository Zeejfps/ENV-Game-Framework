using System.Numerics;
using Bricks.Archetypes;
using Bricks.PhysicsModule;

namespace Bricks.Entities;

public sealed class PaddleEntity : IDynamicEntity, IPaddle
{
    public float HorizontalVelocity { get; private set; }
    public Vector2 CenterPosition { get; private set; }
    public int Width { get; private set; }
    public int Height { get; private set; }
    public const float MaxMovementSpeed = 300f;
    
    public bool MoveLeftInput { get; set; }
    public bool MoveRightInput { get; set; }
    
    private IKeyboard Keyboard { get; }
    private IClock Clock { get; }
    private World World { get; }
    private AABB ArenaBounds => World.Arena;
    
    public PaddleEntity(World world)
    {
        World = world;
        Clock = world.Clock;
        CenterPosition = new Vector2(ArenaBounds.Center.X, ArenaBounds.Bottom - 12.5f);
        Width = 100;
        Height = 25;
    }

    public void Update()
    {
        UpdateHorizontalVelocity();
        UpdatePosition();
        CheckAndResolveCollision();
    }

    private void UpdateHorizontalVelocity()
    {
        HorizontalVelocity = 0;
        if (MoveLeftInput)
        {
            HorizontalVelocity -= MaxMovementSpeed;
        }

        if (MoveRightInput)
        {
            HorizontalVelocity += MaxMovementSpeed;
        }
    }
    
    private void UpdatePosition()
    {
        CenterPosition += Vector2.UnitX * HorizontalVelocity * Clock.DeltaTimeSeconds;
    }

    private void CheckAndResolveCollision()
    {
        CheckAndResolveCanvasCollision();
    }

    private void CheckAndResolveCanvasCollision()
    {
        var bounds = GetAABB();
        if (bounds.Left < ArenaBounds.Left)
        {
            var dx = bounds.Left - ArenaBounds.Left;
            CenterPosition -= Vector2.UnitX * dx;
        }
        else if (bounds.Right > ArenaBounds.Right)
        {
            var dx = bounds.Right - ArenaBounds.Right;
            CenterPosition -= Vector2.UnitX * dx;
        }
    }

    public AABB GetAABB()
    {
        var halfWidth = Width * 0.5f;
        var halfHeight = Height * 0.5f;
        var x = CenterPosition.X - halfWidth;
        var y = CenterPosition.Y - halfHeight;
        return AABB.FromLeftTopWidthHeight(x, y, Width, Height);
    }

    public void Spawn()
    {
        World.DynamicEntities.Add(this);
        World.Paddle = this;
    }

    public void Despawn()
    {
        World.Paddle = null;
        World.DynamicEntities.Remove(this);
    }

    public void Reset()
    {
        CenterPosition = new Vector2(ArenaBounds.Center.X, ArenaBounds.Bottom - 12.5f);
    }
}