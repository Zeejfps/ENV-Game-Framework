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
    private readonly ComponentSystem<Entity, Transform> _transforms;
    private readonly ComponentSystem<Entity, BoxCollider> _boxColliders;
    private readonly ComponentSystem<Entity, CircleCollider> _circleColliders;

    public AabbCollisionSystem(
        Clock clock,
        WorldSystem<Entity> world,
        ComponentSystem<Entity, Rigidbody> rigidbodies,
        ComponentSystem<Entity, Collision> collisions,
        ComponentSystem<Entity, BoxCollider> boxColliders,
        ComponentSystem<Entity, CircleCollider> circleColliders,
        ComponentSystem<Entity, Transform> transforms)
    {
        _clock = clock;
        _world = world;
        _rigidbodies = rigidbodies;
        _collisions = collisions;
        _boxColliders = boxColliders;
        _circleColliders = circleColliders;
        _transforms = transforms;
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
            
            if (!_transforms.TryGetComponent(entity, out var transform))
                continue;
            
            if (!_rigidbodies.TryGetComponent(entity, out var rb))
                continue;
            
            if (rb.IsKinematic)
                continue;
            
            if (!TryGetAabb(entity, transform.Position, out var aabb))
                continue;
            
            var dir = rb.Velocity * _clock.ScaledDeltaTime;
            foreach (var otherEntity in _world.Entities)
            {
                if (otherEntity == entity)
                    continue;
                
                if (!_transforms.TryGetComponent(otherEntity, out var otherTransform))
                    continue;
                
                if (!TryGetAabb(otherEntity, otherTransform.Position, out var otherAabb))
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
            
            CheckLeftWallCollision(entity, aabb, transform, rb, dir);
            CheckRightWallCollision(entity, aabb, transform, rb, dir);
            CheckTopWallCollision(entity, aabb, transform, rb, dir);
        }
    }

    private bool CheckTopWallCollision(Entity entity, AABB aabb, Transform transform, Rigidbody rb, Vector2 dir)
    {
        if (aabb.Top + dir.Y >= 0)
            return false;
        
        var delta = aabb.Top + dir.Y;
        var pos = transform.Position;
        pos.Y -= delta;
        transform.Position = pos;
        var vel = rb.Velocity;
        vel.Y *= -1;
        rb.Velocity = vel;
        _transforms.UpdateComponent(entity, transform);
        _rigidbodies.UpdateComponent(entity, rb);
        SpawnCollisionEntity(entity);
        return true;
    }

    private bool CheckLeftWallCollision(Entity entity, AABB aabb, Transform transform, Rigidbody rb, Vector2 dir)
    {
        if (aabb.Left + dir.X < 0)
        {
            var delta = aabb.Left + dir.X;

            var pos = transform.Position;
            pos.X -= delta;
            transform.Position = pos;
            var vel = rb.Velocity;
            vel.X *= -1;
            rb.Velocity = vel;
            _transforms.UpdateComponent(entity, transform);
            _rigidbodies.UpdateComponent(entity, rb);

            SpawnCollisionEntity(entity);
            return true;
        }
        return false;
    }

    private bool CheckRightWallCollision(Entity entity, AABB aabb, Transform transform, Rigidbody rb, Vector2 dir)
    {
        if (aabb.Right + dir.X < 640)
            return false;
        
        var delta = aabb.Right + dir.X - 640;
        var pos = transform.Position;
        pos.X -= delta;
        transform.Position = pos;
        var vel = rb.Velocity;
        vel.X *= -1;
        rb.Velocity = vel;
        _transforms.UpdateComponent(entity, transform);
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