using System.Numerics;
using EasyGameFramework.Api.Physics;

namespace Pong;

public sealed class Paddle : IPhysicsEntity, IBoxCollider, IPhysicsEntityWithCollider
{
    public Vector2 Position { get; set; }
    public Vector2 PrevPosition { get; set; }
    public float Size { get; } = 10;
    public Rect LevelBounds { get; set; }
    public Vector2 Velocity { get; set; }
    public PhysicsEntity Save()
    {
        return new PhysicsEntity
        {
            Position = Position,
            Velocity = Velocity
        };
    }

    public void Load(PhysicsEntityWithColliderState state)
    {
        Load(state.PhysicsEntityState);
    }

    public void Load(PhysicsEntity state)
    {
        Position = state.Position;
        Velocity = state.Velocity;
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

    PhysicsEntityWithColliderState IPhysicsEntityWithCollider.Save()
    {
        return new PhysicsEntityWithColliderState
        {
            PhysicsEntityState = Save(),
            ColliderState = AABB,
        };
    }
}