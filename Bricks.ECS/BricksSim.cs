using System.Numerics;
using Bricks.PhysicsModule;
using ZGF.ECSModule;

namespace Bricks.ECS;

public sealed class BricksSim : Sim<Entity>
{
    public ComponentSystem<Entity, Rigidbody> Rigidbodies { get; }
    public ComponentSystem<Entity, CircleCollider> CircleColliders { get; }
    public ComponentSystem<Entity, Brick> Bricks { get; }
    public ComponentSystem<Entity, Sprite> Sprites { get; }
    public ComponentSystem<Entity, AABB> Aabbs { get; }
    public ComponentSystem<Entity, Collision> Collisions { get; }
    
    public BricksSim()
    {
        CircleColliders = AddComponentSystem<CircleCollider>();
        Rigidbodies = AddComponentSystem<Rigidbody>();
        Sprites = AddComponentSystem<Sprite>();
        Bricks = AddComponentSystem<Brick>();
        Aabbs = AddComponentSystem<AABB>();
        Collisions = AddComponentSystem<Collision>();
        
        Systems.Add(new BrickSystem(World, Bricks));
        Systems.Add(new PhysicsSystem(Clock, World, Rigidbodies));
        Systems.Add(new AabbUpdaterSystem(World, Aabbs, Rigidbodies, CircleColliders));
        Systems.Add(new AabbCollisionSystem(Clock, World, Rigidbodies, Collisions, Aabbs));
        Systems.Add(new BallBrickCollisionSystem(World, Bricks, Collisions));
        Systems.Add(new SpriteUpdaterSystem(Clock, World, Sprites, Rigidbodies));
        
        SpawnBall();
    }

    public void SpawnBall()
    {
        var circleColliders = CircleColliders;
        var rigidbodies = Rigidbodies;
        var sprites = Sprites;
        var world = World;
        
        var ballEntity = Entity.New();
        
        circleColliders.AddComponent(ballEntity, new CircleCollider
        {
            Radius = 10
        });
        
        rigidbodies.AddComponent(ballEntity, new Rigidbody
        {
            PrevPosition = new Vector2(200, 100),
            Position = new Vector2(200, 100),
            Velocity = new Vector2(-500, 20)
        });
        
        sprites.AddComponent(ballEntity, new Sprite
        {
            Position = new Vector2(200, 100),
            Kind = SpriteKind.Ball,
        });
        
        world.Spawn(ballEntity);
    }
}