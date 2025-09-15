using ZGF.ECSModule;

namespace Bricks.ECS;

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

    protected override void OnPreUpdate()
    {
        base.OnPreUpdate();
        foreach (var kvp in _bricks.UpdatedComponents)
        {
            var entity = kvp.Entity;
            var component = kvp.NewValue;
            if (component.Health <= 0)
            {
                _world.Despawn(entity);
            }
        }
    }
}