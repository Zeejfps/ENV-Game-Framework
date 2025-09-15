using Bricks.ECS.Components;
using ZGF.ECSModule;

namespace Bricks.ECS.Systems;

public sealed class BrickSystem : SystemBase
{
    private readonly WorldSystem<Entity> _world;
    private readonly ComponentSystem<Entity, Brick> _bricks;

    public BrickSystem(
        WorldSystem<Entity> world,
        ComponentSystem<Entity, Brick> bricks)
    {
        _world = world;
        _bricks = bricks;
    }

    protected override void OnUpdate()
    {
        foreach (var entity in _world.Entities)
        {
            if (!_bricks.TryGetComponent(entity, out var brick))
                continue;
            
            if (brick.Health <= 0)
            {
                _world.Despawn(entity);
            }
        }
    }
}