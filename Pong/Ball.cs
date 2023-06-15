using System.Numerics;
using EasyGameFramework.Api;
using EasyGameFramework.Api.Physics;

namespace Pong;

public class Ball : IPhysicsEntity, IPhysicsEntityWithCollider
{
    public Ball(ILogger logger)
    {
        Logger = logger;
    }

    public Vector2 Position { get; set; }
    public Rect Bounds { get; set; }
    public Vector2 Velocity { get; set; } = new(20, 20);
    private ILogger Logger { get; }

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

    PhysicsEntityWithColliderState IPhysicsEntityWithCollider.Save()
    {
        return new PhysicsEntityWithColliderState
        {
            PhysicsEntityState = Save(),
            ColliderState = new Rect
            {
                BottomLeft = Position - new Vector2(0.5f, 0.5f),
                Width = 1f,
                Height = 1f
            }
        };
    }
}