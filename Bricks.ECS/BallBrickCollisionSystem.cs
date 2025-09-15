using ZGF.ECSModule;

namespace Bricks.ECS;

public sealed class BallBrickCollisionSystem : SystemBase
{
    private readonly WorldSystem<Entity> _world;
    private readonly ComponentSystem<Entity, Brick> _bricks;
    private readonly ComponentSystem<Entity, Collision> _collisions;

    public BallBrickCollisionSystem(
        WorldSystem<Entity> world,
        ComponentSystem<Entity, Brick>  bricks, 
        ComponentSystem<Entity, Collision> collisions)
    {
        _world = world;
        _bricks = bricks;
        _collisions = collisions;
    }

    protected override void OnUpdate()
    {
        base.OnUpdate();

        foreach (var entity in _world.Entities)
        {
            if (!_collisions.TryGetComponent(entity, out var collision))
                continue;

            var brickEntity = collision.SecondEntity;
            if (_bricks.TryGetComponent(brickEntity, out var brick))
            {
                brick.Health--;
                _bricks.UpdateComponent(brickEntity, brick);
            }
        }
    }
}