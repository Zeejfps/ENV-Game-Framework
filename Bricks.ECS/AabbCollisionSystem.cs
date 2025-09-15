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
        foreach (var entity in _world.Entities)
        {
            if (_collisions.TryGetComponent(entity, out var collision))
            {
                _collisions.RemoveComponent(entity);
                _world.Despawn(entity);
                continue;
            }
            
            if (!_rigidbodies.TryGetComponent(entity, out var rb))
                continue;
            
            if (rb.IsKinematic)
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

                if (hit.Normal == Vector2.UnitX || hit.Normal == -Vector2.UnitX)
                {
                    var vel = rb.Velocity;
                    vel.X *= -1;
                    rb.Velocity = vel;
                    _rigidbodies.UpdateComponent(entity, rb);
                }
                else if (hit.Normal == Vector2.UnitY || hit.Normal == -Vector2.UnitY)
                {
                    var vel = rb.Velocity;
                    vel.Y *= -1;
                    rb.Velocity = vel;
                    _rigidbodies.UpdateComponent(entity, rb);
                }
            }
            
            CheckLeftWallCollision(entity, aabb, rb, dir);

            if (aabb.Right + dir.X >= 640)
            {
                var delta = aabb.Right + dir.X - 640;
                var pos = rb.Position;
                pos.X -= delta;
                rb.Position = pos;
                var vel = rb.Velocity;
                vel.X *= -1;
                rb.Velocity = vel;
                _rigidbodies.UpdateComponent(entity, rb);
                SpawnCollisionEntity(entity);
                continue;
            }

            if (aabb.Top + dir.Y < 0)
            {
                var delta = aabb.Top + dir.Y;
                var pos = rb.Position;
                pos.Y -= delta;
                rb.Position = pos;
                var vel = rb.Velocity;
                vel.Y *= -1;
                rb.Velocity = vel;
                _rigidbodies.UpdateComponent(entity, rb);
                SpawnCollisionEntity(entity);
                continue;
            } 
        }
    }

    private bool CheckLeftWallCollision(Entity entity, AABB aabb, Rigidbody rb, Vector2 dir)
    {
        if (aabb.Left + dir.X < 0)
        {
            var delta = aabb.Left + dir.X;

            var pos = rb.Position;
            pos.X -= delta;
            rb.Position = pos;
            var vel = rb.Velocity;
            vel.X *= -1;
            rb.Velocity = vel;
            _rigidbodies.UpdateComponent(entity, rb);

            SpawnCollisionEntity(entity);
            return true;
        }
        return false;
    }
    
    private void SpawnCollisionEntity(Entity entity)
    {
        var collisionEntity = Entity.New();
        _collisions.AddComponent(collisionEntity, new Collision
        {
            FirstEntity = entity,
            SecondEntity = Entity.New(),
            Normal = Vector2.UnitX,
        });
        _world.Spawn(collisionEntity);
    }
}