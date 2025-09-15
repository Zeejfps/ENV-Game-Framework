using ZGF.ECSModule;

namespace Bricks.ECS;

public sealed class PhysicsSystem : SystemBase
{
    private readonly WorldSystem<Entity> _world;
    private readonly ComponentSystem<Entity, Rigidbody> _rigidbodies;

    public PhysicsSystem(
        WorldSystem<Entity> world,
        ComponentSystem<Entity, Rigidbody> rigidbodies)
    {
        _world = world;
        _rigidbodies = rigidbodies;
    }

    protected override void OnUpdate()
    {
        base.OnUpdate();
        foreach (var entity in _world.Entities)
        {
            if (!_rigidbodies.TryGetComponent(entity, out var rigidbody))
                continue;

            rigidbody.Position += rigidbody.Velocity;
            _rigidbodies.UpdateComponent(entity, rigidbody);
        }
    }
}