using Bricks.PhysicsModule;
using ZGF.ECSModule;

namespace Bricks.ECS;

public sealed class SpriteUpdaterSystem : SystemBase
{
    private readonly Clock _clock;
    private readonly WorldSystem<Entity> _world;
    private readonly ComponentSystem<Entity, Sprite> _sprites;
    private readonly ComponentSystem<Entity, Rigidbody> _rigidbodies;

    public SpriteUpdaterSystem(
        Clock clock,
        WorldSystem<Entity> world, 
        ComponentSystem<Entity, Sprite> sprites, 
        ComponentSystem<Entity, Rigidbody> rigidbodies)
    {
        _clock = clock;
        _world = world;
        _sprites = sprites;
        _rigidbodies = rigidbodies;
    }

    protected override void OnUpdate()
    {
        base.OnUpdate();
        foreach (var entity in _world.Entities)
        {
            if (!_sprites.TryGetComponent(entity, out var sprite))
                continue;
            
            if (!_rigidbodies.TryGetComponent(entity, out var rigidbody))
                continue;
        }
    }
}