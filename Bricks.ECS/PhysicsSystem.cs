using ZGF.ECSModule;

namespace Bricks.ECS;

public sealed class PhysicsSystem : SystemBase
{
    private readonly Clock _clock;
    private readonly WorldSystem<Entity> _world;
    private readonly ComponentSystem<Entity, Rigidbody> _rigidbodies;

    public PhysicsSystem(
        Clock clock,
        WorldSystem<Entity> world,
        ComponentSystem<Entity, Rigidbody> rigidbodies)
    {
        _clock = clock;
        _world = world;
        _rigidbodies = rigidbodies;
    }

    protected override void OnUpdate()
    {
        foreach (var entity in _world.Entities)
        {
            if (!_rigidbodies.TryGetComponent(entity, out var rigidbody))
                continue;

            rigidbody.PrevPosition = rigidbody.Position;
            rigidbody.Position += rigidbody.Velocity * _clock.ScaledDeltaTime;
            _rigidbodies.UpdateComponent(entity, rigidbody);
        }
    }
}