using Bricks.ECS.Components;
using ZGF.ECSModule;

namespace Bricks.ECS.Systems;

public sealed class NetworkingSystem : SystemBase
{
    private readonly ComponentSystem<Entity, Sprite> _sprites;
    private readonly ComponentSystem<Entity, Transform> _transforms;

    public NetworkingSystem(ComponentSystem<Entity, Sprite> sprites, ComponentSystem<Entity, Transform> transforms)
    {
        _sprites = sprites;
        _transforms = transforms;
    }

    protected override void OnUpdate()
    {
        foreach (var (entity, component) in _sprites.AddedComponents)
        {
            var message = new Message<Sprite>
            {
                Entity = entity,
                Component = component,
                ActionKind = ActionKind.Added,
            };
        }

        foreach (var (entity, component) in _sprites.UpdatedComponents)
        {
            
        }
    }
}

public enum ActionKind
{
    Added,
    Updated,
    Removed,
}

public struct Message<T>
{
    public ActionKind ActionKind { get; set; }
    public Entity Entity { get; set; }
    public T Component { get; set; }
}