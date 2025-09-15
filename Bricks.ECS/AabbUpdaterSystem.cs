using Bricks.PhysicsModule;
using ZGF.ECSModule;

namespace Bricks.ECS;

public sealed class AabbUpdaterSystem : SystemBase
{
    private readonly WorldSystem<Entity> _world;
    private readonly ComponentSystem<Entity, Rigidbody> _rigidbodies;
    private readonly ComponentSystem<Entity, AABB> _aabbs;
    private readonly ComponentSystem<Entity, CircleCollider> _circleColliders; 
    private readonly ComponentSystem<Entity, BoxCollider> _boxColliders; 

    public AabbUpdaterSystem(
        WorldSystem<Entity> world,
        ComponentSystem<Entity, AABB> aabbs, 
        ComponentSystem<Entity, Rigidbody> rigidbodies,
        ComponentSystem<Entity, CircleCollider> circleColliders,
        ComponentSystem<Entity, BoxCollider> boxColliders)
    {
        _world = world;
        _aabbs = aabbs;
        _rigidbodies = rigidbodies;
        _circleColliders = circleColliders;
        _boxColliders = boxColliders;
    }

    protected override void OnUpdate()
    {
        base.OnUpdate();
        foreach (var entity in _world.Entities)
        {
            if (!_rigidbodies.TryGetComponent(entity, out var rigidbody))
                continue;

            var pos = rigidbody.Position;
            if (_circleColliders.TryGetComponent(entity, out var circleCollider))
            {
                var radius = circleCollider.Radius;
                var aabb = AABB.FromLeftTopWidthHeight(
                    pos.X - radius * 0.5f,
                    pos.Y + radius * 0.5f,
                    radius,
                    radius
                );
                
                _aabbs.UpdateComponent(entity, aabb);
                continue;
            }
            
            if (_boxColliders.TryGetComponent(entity, out var boxCollider))
            {
                var w = boxCollider.Width;
                var h = boxCollider.Height;
                var aabb = AABB.FromLeftTopWidthHeight(
                    pos.X - w*0.5f,
                    pos.Y + h * 0.5f,
                    w,
                    h
                );
                
                _aabbs.UpdateComponent(entity, aabb);
                continue;
            }
        }
    }
}