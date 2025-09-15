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
    public AABB Bounds { get; }
    
    public BricksSim(AABB bounds)
    {
        Bounds = bounds;
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
        
        SpawnBall();
        SpawnBricks();
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
            Position = new Vector2(200, 100),
            Velocity = new Vector2(-500, -50),
            IsKinematic = false
        });
        
        sprites.AddComponent(ballEntity, new Sprite
        {
            Kind = SpriteKind.Ball,
        });
        
        world.Spawn(ballEntity);
    }
    
    private void SpawnBricks()
    {
        var arena = Bounds;
        var leftPadding = 10;
        var rightPadding = 10;
        var topPadding = 10;
        var horizontalGap = 5;
        var verticalGap = 5;
        var bricksPerRowCount = 8;
        var brickRowsCount = 4;
        var brickHeight = 30;
        var rowWidth = arena.Width - leftPadding - rightPadding - (bricksPerRowCount-1) * horizontalGap;
        var brickWidth = rowWidth / bricksPerRowCount;
        var brickHalfWidth = brickWidth * 0.5f;
        var brickHalfHeight = brickHeight * 0.5f;

        for (var i = 0; i < brickRowsCount; i++)
        {
            var y = (i * brickHeight) + (i * verticalGap) + brickHalfHeight + topPadding;
            for (var j = 0; j < bricksPerRowCount; j++)
            {
                var x = (j * brickWidth) + (j * horizontalGap) + brickHalfWidth + leftPadding;
                SpawnBrick(x, y, brickWidth, brickHeight);
            }
        }
    }

    private void SpawnBrick(float x, float y, float width, float height)
    {
        var entity = Entity.New();
        Sprites.AddComponent(entity, new Sprite
        {
            Kind = SpriteKind.Brick
        });
        Rigidbodies.AddComponent(entity, new Rigidbody
        {
            IsKinematic = true,
            Position = new Vector2(x, y),
            Velocity = Vector2.Zero
        });
    }
}
