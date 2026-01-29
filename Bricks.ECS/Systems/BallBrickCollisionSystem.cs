using Bricks.ECS.Components;
using ZGF.ECSModule;

namespace Bricks.ECS.Systems;

public sealed class BallBrickCollisionSystem : SystemBase
{
    private readonly WorldSystem _world;
    private readonly ComponentSystem<Brick> _bricks;
    private readonly ComponentSystem<Collision> _collisions;

    public BallBrickCollisionSystem(
        WorldSystem world,
        ComponentSystem<Brick>  bricks, 
        ComponentSystem<Collision> collisions)
    {
        _world = world;
        _bricks = bricks;
        _collisions = collisions;
    }

    protected override void OnUpdate()
    {
        foreach (var entity in _world.Entities)
        {
            if (!_collisions.TryGetComponent(entity, out var collision))
                continue;

            var brickEntity = collision.SecondEntity;
            if (!_bricks.TryGetComponent(brickEntity, out var brick)) 
                continue;
            
            brick.Health--;
            _bricks.UpdateComponent(brickEntity, brick);
        }
    }
}