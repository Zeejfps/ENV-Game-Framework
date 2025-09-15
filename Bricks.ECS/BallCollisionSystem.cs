using ZGF.ECSModule;

namespace Bricks.ECS;

public sealed class BallCollisionSystem : SystemBase
{
    private readonly WorldSystem<Entity> _world;
    private readonly ComponentSystem<Entity, Brick> _bricks;
    private readonly List<Collision> _collisions = new();

    public BallCollisionSystem(
        WorldSystem<Entity> world,
        ComponentSystem<Entity, Brick>  bricks)
    {
        _world = world;
        _bricks = bricks;
    }

    public void AddCollision(Collision collision)
    {
        _collisions.Add(collision);
    }

    protected override void OnUpdate()
    {
        base.OnUpdate();

        foreach (var collision in _collisions)
        {
            var other = collision.SecondEntity;
            if (_bricks.TryGetComponent(other, out var brick))
            {
                brick.Health--;
                _bricks.UpdateComponent(other, brick);
            }
        }
        
        _collisions.Clear();
    }
}