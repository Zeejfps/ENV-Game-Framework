using System.Numerics;
using Bricks.Archetypes;

namespace Bricks.Entities;

public sealed class PaddleEntity : IDynamicEntity
{
    public float HorizontalVelocity { get; private set; }
    public Vector2 CenterPosition { get; private set; }
    public int Width { get; private set; }
    public int Height { get; private set; }
    public const float MaxMovementSpeed = 300f;
    
    private IInput Input { get; }
    private IClock Clock { get; }
    private Game Game { get; }
    private AABB ArenaBounds { get; }
    
    public PaddleEntity(IInput input, Game game, AABB arenaBounds)
    {
        Input = input;
        Game = game;
        Clock = game.Clock;
        ArenaBounds = arenaBounds;
        CenterPosition = new Vector2(ArenaBounds.Center.X, ArenaBounds.Bottom - 50);
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
        if (Input.IsKeyDown(KeyCode.A))
        {
            HorizontalVelocity -= MaxMovementSpeed;
        }
        if (Input.IsKeyDown(KeyCode.D))
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
        Game.DynamicEntities.Add(this);
        Game.Paddle = this;
    }

    public void Despawn()
    {
        Game.Paddle = null;
        Game.DynamicEntities.Remove(this);
    }
}