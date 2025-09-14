using System.Numerics;
using ZGF.ECSModule;

namespace Bricks.ECS;

public sealed class BricksGame
{
    public WorldSystem<Entity> World { get; }
    public ComponentSystem<Entity, Rigidbody> Rigidbodies { get; }
    public ComponentSystem<Entity, CircleCollider> CircleColliders { get; }
    public List<ISystem> Systems { get; }
    
    public BricksGame()
    {
        CircleColliders = new ComponentSystem<Entity, CircleCollider>();
        Rigidbodies = new ComponentSystem<Entity, Rigidbody>();
        World = new WorldSystem<Entity>();

        SpawnBall(World, CircleColliders, Rigidbodies);
        
        Systems =
        [
            World,
            CircleColliders,
            Rigidbodies
        ];
    }

    private void SpawnBall(
        WorldSystem<Entity> world,
        ComponentSystem<Entity, CircleCollider> circleColliders,
        ComponentSystem<Entity, Rigidbody> transforms)
    {
        var ballEntity = Entity.New();
        ballEntity.Tags = Tags.Ball;
        
        circleColliders.AddComponent(ballEntity, new CircleCollider
        {
            Radius = 1
        });
        
        transforms.AddComponent(ballEntity, new Rigidbody
        {
            Position = new Vector2(0, 0),
            Velocity = new Vector2(2, 2)
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
        foreach (var system in Systems)
        {
            system.PreUpdate();
        }
        
        foreach (var system in Systems)
        {
            system.Update();
        }

        foreach (var system in Systems)
        {
            system.PostUpdate();
        }
    }
}