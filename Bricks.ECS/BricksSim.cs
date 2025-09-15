using System.Numerics;
using ZGF.ECSModule;

namespace Bricks.ECS;

public sealed class BricksSim
{
    private readonly BallCollisionSystem _ballCollisionSystem;
    
    public WorldSystem<Entity> World { get; }
    public ComponentSystem<Entity, Rigidbody> Rigidbodies { get; }
    public ComponentSystem<Entity, CircleCollider> CircleColliders { get; }
    public ComponentSystem<Entity, Brick> Bricks { get; }
    public ComponentSystem<Entity, Renderable> Renderables { get; }
    public List<ISystem> Systems { get; }
    
    public BricksSim()
    {
        CircleColliders = new ComponentSystem<Entity, CircleCollider>();
        Rigidbodies = new ComponentSystem<Entity, Rigidbody>();
        Bricks = new ComponentSystem<Entity, Brick>();
        Renderables = new ComponentSystem<Entity, Renderable>();
        World = new WorldSystem<Entity>();
        
        _ballCollisionSystem = new BallCollisionSystem(World, Bricks);
        
        Systems =
        [
            World,
            CircleColliders,
            Rigidbodies,
            Bricks,
            Renderables,
            _ballCollisionSystem,
            new BrickSystem(World, Bricks)
        ];
        
        SpawnBall();
    }

    public void SpawnBall()
    {
        var circleColliders = CircleColliders;
        var rigidbodies = Rigidbodies;
        var renderables = Renderables;
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
        
        renderables.AddComponent(ballEntity, new Renderable
        {
            Kind = RenderableKind.Ball,
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