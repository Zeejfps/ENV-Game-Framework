using System.Numerics;
using Bricks.ECS.Components;
using Bricks.ECS.Systems;
using Bricks.PhysicsModule;
using ZGF.ECSModule;

namespace Bricks.ECS;

public sealed class BricksSim : Sim<Entity>
{
    public ComponentSystem<Entity, Rigidbody> Rigidbodies { get; }
    public ComponentSystem<Entity, CircleCollider> CircleColliders { get; }
    public ComponentSystem<Entity, BoxCollider> BoxColliders { get; }
    public ComponentSystem<Entity, Brick> Bricks { get; }
    public ComponentSystem<Entity, Sprite> Sprites { get; }
    public ComponentSystem<Entity, AABB> Aabbs { get; }
    public ComponentSystem<Entity, Collision> Collisions { get; }
    public AABB Bounds { get; }
    
    public BricksSim(AABB bounds)
    {
        Bounds = bounds;
        CircleColliders = AddComponentSystem<CircleCollider>();
        BoxColliders = AddComponentSystem<BoxCollider>();
        Rigidbodies = AddComponentSystem<Rigidbody>();
        Sprites = AddComponentSystem<Sprite>();
        Bricks = AddComponentSystem<Brick>();
        Aabbs = AddComponentSystem<AABB>();
        Collisions = AddComponentSystem<Collision>();
        
        Systems.Add(new BrickSystem(World, Bricks));
        Systems.Add(new PhysicsSystem(Clock, World, Rigidbodies));
        Systems.Add(new AabbUpdaterSystem(World, Aabbs, Rigidbodies, CircleColliders, BoxColliders));
        Systems.Add(new AabbCollisionSystem(Clock, World, Rigidbodies, Collisions, Aabbs));
        Systems.Add(new BallBrickCollisionSystem(World, Bricks, Collisions));
        
        SpawnBall();
        SpawnBricks();
        //SpawnPaddle();
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
            Radius = 20
        });
        
        rigidbodies.AddComponent(ballEntity, new Rigidbody
        {
            Position = new Vector2(320, 400),
            Velocity = new Vector2((Random.Shared.NextSingle() * 2 - 1) * 500, Random.Shared.NextSingle() * -500),
            // Velocity = new Vector2(0f, -400f),
            IsKinematic = false
        });
        
        sprites.AddComponent(ballEntity, new Sprite
        {
            Kind = SpriteKind.Ball,
            Width = 20,
            Height = 20
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
            Kind = SpriteKind.Brick,
            Width = width,
            Height = height
        });
        Rigidbodies.AddComponent(entity, new Rigidbody
        {
            IsKinematic = true,
            Position = new Vector2(x, y),
            Velocity = Vector2.Zero
        });
        BoxColliders.AddComponent(entity, new BoxCollider
        {
            Width = width,
            Height = height
        });
        
        Bricks.AddComponent(entity, new Brick
        {
            Health = 1
        });
        
        World.Spawn(entity);
    }
    
    private void SpawnPaddle()
    {
        var entity = Entity.New();
        
        Sprites.AddComponent(entity, new Sprite
        {
            Kind = SpriteKind.Paddle,
            Width = 120,
            Height = 25
        });
        
        Rigidbodies.AddComponent(entity, new Rigidbody
        {
            IsKinematic = false,
            Position = new Vector2(320, 400),
            Velocity = Vector2.Zero
        });
        
        BoxColliders.AddComponent(entity, new BoxCollider
        {
            Width = 120,
            Height = 25
        });
        
        World.Spawn(entity);
    }
}
