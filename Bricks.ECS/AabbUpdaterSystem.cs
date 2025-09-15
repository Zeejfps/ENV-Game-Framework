using Bricks.PhysicsModule;
using ZGF.ECSModule;

namespace Bricks.ECS;

public sealed class AabbUpdaterSystem : SystemBase
{
    private readonly WorldSystem<Entity> _world;
    private readonly ComponentSystem<Entity, Rigidbody> _rigidbodies;
    private readonly ComponentSystem<Entity, AABB> _aabbs;
    private readonly ComponentSystem<Entity, CircleCollider> _circleColliders; 

    public AabbUpdaterSystem(
        WorldSystem<Entity> world,
        ComponentSystem<Entity, AABB> aabbs, 
        ComponentSystem<Entity, Rigidbody> rigidbodies,
        ComponentSystem<Entity, CircleCollider> circleColliders)
    {
        _world = world;
        _aabbs = aabbs;
        _rigidbodies = rigidbodies;
        _circleColliders = circleColliders;
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
                    pos.X - radius,
                    pos.Y + radius,
                    radius * 2,
                    radius * 2
                );
                
                _aabbs.UpdateComponent(entity, aabb);
                continue;
            }
        }
    }
}