using System.Numerics;
using Bricks.PhysicsModule;
using ZGF.ECSModule;

namespace Bricks.ECS;

public sealed class AabbCollisionSystem : SystemBase
{
    private readonly Clock _clock;
    private readonly WorldSystem<Entity> _world;
    private readonly ComponentSystem<Entity, Rigidbody> _rigidbodies;
    private readonly ComponentSystem<Entity, Collision> _collisions;
    private readonly ComponentSystem<Entity, AABB> _aabbs;

    public AabbCollisionSystem(
        Clock clock,
        WorldSystem<Entity> world,
        ComponentSystem<Entity, Rigidbody> rigidbodies,
        ComponentSystem<Entity, Collision> collisions, 
        ComponentSystem<Entity, AABB> aabbs)
    {
        _clock = clock;
        _world = world;
        _rigidbodies = rigidbodies;
        _collisions = collisions;
        _aabbs = aabbs;
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
            
            if (!_rigidbodies.TryGetComponent(entity, out var rb))
                continue;
            
            if (!_aabbs.TryGetComponent(entity, out var aabb))
                continue;

            var dir = rb.Velocity * _clock.ScaledDeltaTime;
            foreach (var otherEntity in _world.Entities)
            {
                if (otherEntity == entity)
                    continue;
                
                if (!_aabbs.TryGetComponent(otherEntity, out var otherAabb))
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