using System.Numerics;
using ZGF.ECSModule;

namespace Bricks.ECS;

public sealed class BricksGame
{
    public WorldSystem<Entity> World { get; }
    public ComponentSystem<Entity, Rigidbody> Rigidbodies { get; }
    public ComponentSystem<Entity, CircleCollider> Colliders { get; }

    private readonly ISystem[] _systems;
    
    public BricksGame()
    {
        Colliders = new ComponentSystem<Entity, CircleCollider>();
        Rigidbodies = new ComponentSystem<Entity, Rigidbody>();
        World = new WorldSystem<Entity>();

        SpawnBall(World, Colliders, Rigidbodies);
        
        _systems =
        [
            World
        ];
    }

    private void SpawnBall(
        WorldSystem<Entity> world,
        ComponentSystem<Entity, CircleCollider> colliders,
        ComponentSystem<Entity, Rigidbody> transforms)
    {
        var ballEntity = Entity.New();
        ballEntity.Tags = Tags.Ball;
        
        colliders.AddComponent(ballEntity, new CircleCollider
        {
            Radius = 1
        });
        
        transforms.AddComponent(ballEntity, new Rigidbody
        {
            Position = new Vector2(0, 0),
            Velocity = Vector2.Zero
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