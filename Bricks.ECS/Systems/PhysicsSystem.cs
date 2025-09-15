using Bricks.ECS.Components;
using ZGF.ECSModule;

namespace Bricks.ECS.Systems;

public sealed class PhysicsSystem : SystemBase
{
    private readonly Clock _clock;
    private readonly WorldSystem<Entity> _world;
    private readonly ComponentSystem<Entity, Rigidbody> _rigidbodies;
    private readonly ComponentSystem<Entity, Transform> _transforms;

    public PhysicsSystem(
        Clock clock,
        WorldSystem<Entity> world,
        ComponentSystem<Entity, Rigidbody> rigidbodies, 
        ComponentSystem<Entity, Transform> transforms)
    {
        _clock = clock;
        _world = world;
        _rigidbodies = rigidbodies;
        _transforms = transforms;
    }

    protected override void OnUpdate()
    {
        foreach (var entity in _world.Entities)
        {
            if (!_rigidbodies.TryGetComponent(entity, out var rigidbody))
                continue;

            if (rigidbody.IsKinematic)
                continue;
            
            if (!_transforms.TryGetComponent(entity, out var transform))
                continue;
            
            transform.Position += rigidbody.Velocity * _clock.ScaledDeltaTime;
            _transforms.UpdateComponent(entity, transform);
        }
    }
}