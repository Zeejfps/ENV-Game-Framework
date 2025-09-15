using System.Numerics;
using Bricks.PhysicsModule;
using ZGF.ECSModule;

namespace Bricks.ECS;

public sealed class BricksSim : Sim<Entity>
{
    public ComponentSystem<Entity, Rigidbody> Rigidbodies { get; }
    public ComponentSystem<Entity, CircleCollider> CircleColliders { get; }
    public ComponentSystem<Entity, Brick> Bricks { get; }
    public ComponentSystem<Entity, Renderable> Renderables { get; }
    public ComponentSystem<Entity, AABB> Aabbs { get; }
    public ComponentSystem<Entity, Collision> Collisions { get; }
    
    public BricksSim()
    {
        CircleColliders = AddComponentSystem<CircleCollider>();
        Rigidbodies = AddComponentSystem<Rigidbody>();
        Renderables = AddComponentSystem<Renderable>();
        Bricks = AddComponentSystem<Brick>();
        Aabbs = AddComponentSystem<AABB>();
        Collisions = AddComponentSystem<Collision>();
        
        Systems.Add(new BrickSystem(World, Bricks));
        Systems.Add(new PhysicsSystem(Clock, World, Rigidbodies));
        Systems.Add(new AabbUpdaterSystem(World, Aabbs, Rigidbodies, CircleColliders));
        Systems.Add(new AabbCollisionSystem(Clock, World, Rigidbodies, Collisions, Aabbs));
        Systems.Add(new BallBrickCollisionSystem(World, Bricks, Collisions));
        
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
            Radius = 10
        });
        
        rigidbodies.AddComponent(ballEntity, new Rigidbody
        {
            Position = new Vector2(200, 100),
            Velocity = new Vector2(-500, 20)
        });
        
        renderables.AddComponent(ballEntity, new Renderable
        {
            Kind = RenderableKind.Ball,
        });
        
        world.Spawn(ballEntity);
    }
}