using System.Numerics;
using ZGF.ECSModule;

namespace Bricks.ECS;

public sealed class BricksGame
{
    private readonly BallCollisionSystem _ballCollisionSystem;
    
    public WorldSystem<Entity> World { get; }
    public ComponentSystem<Entity, Rigidbody> Rigidbodies { get; }
    public ComponentSystem<Entity, CircleCollider> CircleColliders { get; }
    public ComponentSystem<Entity, Brick> Bricks { get; }
    public List<ISystem> Systems { get; }
    
    public BricksGame()
    {
        CircleColliders = new ComponentSystem<Entity, CircleCollider>();
        Rigidbodies = new ComponentSystem<Entity, Rigidbody>();
        Bricks = new ComponentSystem<Entity, Brick>();
        World = new WorldSystem<Entity>();
        
        _ballCollisionSystem = new BallCollisionSystem(World, Bricks);
        
        Systems =
        [
            World,
            CircleColliders,
            Rigidbodies,
            _ballCollisionSystem,
            new BrickSystem(World, Bricks)
        ];
        
        SpawnBall();
    }

    public void SpawnBall()
    {
        var circleColliders = CircleColliders;
        var rigidbodies = Rigidbodies;
        var world = World;
        
        var ballEntity = Entity.New();
        
        circleColliders.AddComponent(ballEntity, new CircleCollider
        {
            Radius = 1
        });
        
        rigidbodies.AddComponent(ballEntity, new Rigidbody
        {
            Position = new Vector2(0, 0),
            Velocity = new Vector2(2, 2)
        });
        
        world.Spawn(ballEntity);
    }

    public void AddBallCollision(BallCollision ballCollision)
    {
        _ballCollisionSystem.AddCollision(ballCollision);
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