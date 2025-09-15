using System.Numerics;
using Bricks.ECS.Components;
using Bricks.PhysicsModule;
using ZGF.ECSModule;

namespace Bricks.ECS.Systems;

public sealed class AabbCollisionSystem : SystemBase
{
    private readonly Clock _clock;
    private readonly WorldSystem<Entity> _world;
    private readonly ComponentSystem<Entity, Rigidbody> _rigidbodies;
    private readonly ComponentSystem<Entity, Collision> _collisions;
    private readonly ComponentSystem<Entity, BoxCollider> _boxColliders;
    private readonly ComponentSystem<Entity, CircleCollider> _circleColliders;

    public AabbCollisionSystem(
        Clock clock,
        WorldSystem<Entity> world,
        ComponentSystem<Entity, Rigidbody> rigidbodies,
        ComponentSystem<Entity, Collision> collisions,
        ComponentSystem<Entity, BoxCollider> boxColliders,
        ComponentSystem<Entity, CircleCollider> circleColliders)
    {
        _clock = clock;
        _world = world;
        _rigidbodies = rigidbodies;
        _collisions = collisions;
        _boxColliders = boxColliders;
        _circleColliders = circleColliders;
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
            
            if (!TryGetAabb(entity, rb.Position, out var aabb))
                continue;
            
            var dir = rb.Velocity * _clock.ScaledDeltaTime;
            foreach (var otherEntity in _world.Entities)
            {
                if (otherEntity == entity)
                    continue;
                
                if (!_rigidbodies.TryGetComponent(otherEntity, out var otherRb))
                    continue;
                
                if (!TryGetAabb(otherEntity, otherRb.Position, out var otherAabb))
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
                else if (hit.Normal == Vector2.UnitY)
                {
                    var vel = rb.Velocity;
                    vel.Y = MathF.Abs(vel.Y);
                    rb.Velocity = vel;
                    _rigidbodies.UpdateComponent(entity, rb);
                }
                else if (hit.Normal == -Vector2.UnitY)
                {
                    var vel = rb.Velocity;
                    vel.Y = MathF.Abs(vel.Y) * -1;
                    rb.Velocity = vel;
                    _rigidbodies.UpdateComponent(entity, rb);
                }
            }
            
            CheckLeftWallCollision(entity, aabb, rb, dir);
            CheckRightWallCollision(entity, aabb, rb, dir);
            CheckTopWallCollision(entity, aabb, rb, dir);
        }
    }

    private bool CheckTopWallCollision(Entity entity, AABB aabb, Rigidbody rb, Vector2 dir)
    {
        if (aabb.Top + dir.Y >= 0)
            return false;
        
        var delta = aabb.Top + dir.Y;
        var pos = rb.Position;
        pos.Y -= delta;
        rb.Position = pos;
        var vel = rb.Velocity;
        vel.Y *= -1;
        rb.Velocity = vel;
        _rigidbodies.UpdateComponent(entity, rb);
        SpawnCollisionEntity(entity);
        return true;
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

    private bool CheckRightWallCollision(Entity entity, AABB aabb, Rigidbody rb, Vector2 dir)
    {
        if (aabb.Right + dir.X < 640)
            return false;
        
        var delta = aabb.Right + dir.X - 640;
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

    private bool TryGetAabb(Entity entity, Vector2 pos, out AABB aabb)
    {
        if (_circleColliders.TryGetComponent(entity, out var circleCollider))
        {
            var radius = circleCollider.Radius;
            aabb = AABB.FromLeftTopWidthHeight(
                pos.X - radius * 0.5f,
                pos.Y + radius * 0.5f,
                radius,
                radius
            );
            return true;
        }
            
        if (_boxColliders.TryGetComponent(entity, out var boxCollider))
        {
            var w = boxCollider.Width;
            var h = boxCollider.Height;
            aabb = AABB.FromLeftTopWidthHeight(
                pos.X - w*0.5f,
                pos.Y - h * 0.5f,
                w,
                h
            );
            return true;
        }

        aabb = default;
        return false;
    }
}