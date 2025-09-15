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
    private readonly ComponentSystem<Entity, Collision> _collisions;

    public AabbCollisionSystem(
        Clock clock,
        WorldSystem<Entity> world,
        ComponentSystem<Entity, Rigidbody> rigidbodies,
        ComponentSystem<Entity, CircleCollider> circleColliders, 
        ComponentSystem<Entity, Collision> collisions)
    {
        _clock = clock;
        _world = world;
        _rigidbodies = rigidbodies;
        _circleColliders = circleColliders;
        _collisions = collisions;
    }

    protected override void OnUpdate()
    {
        base.OnUpdate();
        foreach (var entity in _world.Entities)
        {
            if (_collisions.TryGetComponent(entity, out var collision))
            {
                ResolveCollision(collision);
                _collisions.RemoveComponent(entity);
                _world.Despawn(entity);
                continue;
            }
            
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

                var collisionEntity = Entity.New();
                _collisions.AddComponent(collisionEntity, new Collision
                {
                    FirstEntity = entity,
                    SecondEntity = otherEntity,
                    Normal = hit.Normal,
                });
                _world.Spawn(collisionEntity);
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

    private void ResolveCollision(Collision collision)
    {
        var ballEntity = collision.FirstEntity;
        
        if (collision.Normal == Vector2.UnitX || collision.Normal == -Vector2.UnitX)
            ReflectVelocityX(ballEntity);
        
        ReflectVelocityY(ballEntity);
    }
    
    private void ReflectVelocityX(Entity entity)
    {
        if (!_rigidbodies.TryGetComponent(entity, out var rigidbody))
            return;
        
        rigidbody.Velocity = rigidbody.Velocity with { X = rigidbody.Velocity.X * -1f };
        _rigidbodies.UpdateComponent(entity, rigidbody);
    }

    private void ReflectVelocityY(Entity entity)
    {
        if (!_rigidbodies.TryGetComponent(entity, out var rb))
            return;
        
        rb.Velocity = rb.Velocity with { Y = rb.Velocity.Y * -1f };
        _rigidbodies.UpdateComponent(entity, rb);
    }
}