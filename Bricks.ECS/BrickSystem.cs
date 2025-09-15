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

    protected override void OnPostUpdate()
    {
        foreach (var (entity, updatedComponent) in _bricks.UpdatedComponents)
        {
            var component = updatedComponent.NewValue;
            if (component.Health <= 0)
            {
                _world.Despawn(entity);
            }
        }
    }
}