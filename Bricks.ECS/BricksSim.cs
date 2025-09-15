using System.Numerics;
using ZGF.ECSModule;

namespace Bricks.ECS;

public sealed class BricksSim : Sim<Entity>
{
    private readonly BallCollisionSystem _ballCollisionSystem;
    private readonly BrickSystem _brickSystem;

    public ComponentSystem<Entity, Rigidbody> Rigidbodies { get; }
    public ComponentSystem<Entity, CircleCollider> CircleColliders { get; }
    public ComponentSystem<Entity, Brick> Bricks { get; }
    public ComponentSystem<Entity, Renderable> Renderables { get; }
    
    public BricksSim()
    {
        CircleColliders = AddComponentSystem<CircleCollider>();
        Rigidbodies = AddComponentSystem<Rigidbody>();
        Renderables = AddComponentSystem<Renderable>();
        Bricks = AddComponentSystem<Brick>();
        
        _ballCollisionSystem = new BallCollisionSystem(World, Bricks);
        _brickSystem = new BrickSystem(World, Bricks);

        Systems.Add(_ballCollisionSystem);
        Systems.Add(_brickSystem);
        Systems.Add(new PhysicsSystem(Clock, World, Rigidbodies));
        
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

    public void AddBallCollision(Collision collision)
    {
        _ballCollisionSystem.AddCollision(collision);
    }
}