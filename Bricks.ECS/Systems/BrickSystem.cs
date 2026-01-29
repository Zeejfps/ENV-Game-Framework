using Bricks.ECS.Components;
using ZGF.ECSModule;

namespace Bricks.ECS.Systems;

public sealed class BrickSystem : SystemBase
{
    private readonly WorldSystem _world;
    private readonly ComponentSystem<Brick> _bricks;

    public BrickSystem(
        WorldSystem world,
        ComponentSystem<Brick> bricks)
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