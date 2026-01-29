using ZGF.ECSModule;

namespace Bricks.ECS.Systems;

public sealed class BallBrickCollisionSystem : SystemBase
{
    private readonly BricksSim _sim;

    public BallBrickCollisionSystem(BricksSim sim) {
        _sim = sim;
    }

    protected override void OnUpdate()
    {
        var world = _sim.World;
        var collisions = _sim.Collisions;
        var bricks = _sim.Bricks;
        
        foreach (var entity in world.Entities)
        {
            if (!collisions.TryGetComponent(entity, out var collision))
                continue;

            var brickEntity = collision.SecondEntity;
            if (!bricks.TryGetComponent(brickEntity, out var brick)) 
                continue;
            
            brick.Health--;
            bricks.UpdateComponent(brickEntity, brick);
        }
    }
}