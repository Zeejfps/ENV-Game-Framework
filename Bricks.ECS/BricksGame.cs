using System.Numerics;
using ZGF.ECSModule;

namespace Bricks.ECS;

public sealed class BricksGame
{
    private readonly ISystem[] _systems;

    public BricksGame()
    {
        var colliders = new ComponentSystem<Entity, CircleCollider>();
        var transforms = new ComponentSystem<Entity, Transform>();
        var world = new WorldSystem<Entity>();

        SpawnBall(world, colliders, transforms);
        
        _systems =
        [
            world
        ];
    }

    private void SpawnBall(
        WorldSystem<Entity> world,
        ComponentSystem<Entity, CircleCollider> colliders,
        ComponentSystem<Entity, Transform> transforms)
    {
        var ballEntity = Entity.New();
        ballEntity.Tags = Tags.Ball;
        
        colliders.AddComponent(ballEntity, new CircleCollider
        {
            Radius = 1
        });
        
        transforms.AddComponent(ballEntity, new Transform
        {
            Position = new Vector2(0, 0)
        });
        
        world.Spawn(ballEntity);
    }

    public void Resume()
    {
        
    }

    public void Pause()
    {
        
    }

    public void Update(float dt)
    {
        foreach (var system in _systems)
        {
            system.PreUpdate();
        }
        
        foreach (var system in _systems)
        {
            system.Update();
        }

        foreach (var system in _systems)
        {
            system.PostUpdate();
        }
    }
}