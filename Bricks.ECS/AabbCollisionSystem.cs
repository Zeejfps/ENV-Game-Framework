using System.Numerics;
using Bricks.PhysicsModule;
using ZGF.ECSModule;

namespace Bricks.ECS;

public sealed class AabbCollisionSystem : SystemBase
{
    private readonly Clock _clock;
    private readonly WorldSystem<Entity> _world;
    private readonly ComponentSystem<Entity, Rigidbody> _rigidbodies;
    private readonly ComponentSystem<Entity, CircleCollider> _circleColliders;

    public AabbCollisionSystem(
        Clock clock,
        WorldSystem<Entity> world,
        ComponentSystem<Entity, Rigidbody> rigidbodies,
        ComponentSystem<Entity, CircleCollider> circleColliders)
    {
        _clock = clock;
        _world = world;
        _rigidbodies = rigidbodies;
        _circleColliders = circleColliders;
    }

    protected override void OnUpdate()
    {
        base.OnUpdate();
        foreach (var entity in _world.Entities)
        {
            if (!TryGetAabb(entity, out var rb, out var aabb))
                continue;

            var dir = rb.Velocity * _clock.ScaledDeltaTime;
            foreach (var otherEntity in _world.Entities)
            {
                if (otherEntity == entity)
                    continue;
                
                if (!TryGetAabb(otherEntity, out var otherRb, out var otherAabb))
                    continue;

                if (!aabb.TryCast(dir, otherAabb, out var hit))
                    continue;
            }
        }
    }

    private bool TryGetAabb(Entity entity, out Rigidbody rigidbody, out AABB aabb)
    {
        aabb = default;
        rigidbody = default;
        if (!_rigidbodies.TryGetComponent(entity, out rigidbody))
            return false;

        var pos = rigidbody.Position;
        
        if (_circleColliders.TryGetComponent(entity, out var circleCollider))
        {
            var radius = circleCollider.Radius;
            aabb = AABB.FromLeftTopWidthHeight(
                pos.X - radius,
                pos.Y + radius,
                radius * 2,
                radius * 2
            );
            
            return true;
        }

        return false;
    }
}