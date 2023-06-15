using System.Numerics;
using EasyGameFramework.Api.Physics;

namespace Pong;

public sealed class Paddle : IPhysicsEntity, IBoxCollider
{
    public Vector2 Position { get; set; }
    public Vector2 PrevPosition { get; set; }
    public float Size { get; } = 10;
    public Rect LevelBounds { get; set; }
    public Vector2 Velocity { get; set; }

    public void MoveLeft(float xDelta)
    {
        var newPositionX = Position.X - xDelta;
        if (newPositionX - Size < LevelBounds.Left)
            newPositionX = LevelBounds.Left + Size;
        Position = Position with { X = newPositionX };
    }

    public void MoveRight(float xDelta)
    {
        var newPositionX = Position.X + xDelta;
        if (newPositionX + Size > LevelBounds.Right)
            newPositionX = LevelBounds.Right - Size;
        Position = Position with { X = newPositionX };
    }

    public Rect AABB =>
        new()
        {
            BottomLeft = new Vector2(
                Position.X - Size - 0.5f,
                Position.Y - 1f - 0.5f
            ),
            Width = Size * 2f + 1f,
            Height = 2 + 1f
        };
}