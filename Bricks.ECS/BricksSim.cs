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
    public ComponentSystem<Entity, Collision> Collisions { get; }
    public ComponentSystem<Entity, Transform> Transforms { get; }
    public AABB Bounds { get; }
    
    private Entity Paddle { get; set; }
    private bool _movePaddleLeft;
    private bool _movePaddleRight;
    
    public BricksSim(AABB bounds)
    {
        Bounds = bounds;
        CircleColliders = AddComponentSystem<CircleCollider>();
        BoxColliders = AddComponentSystem<BoxCollider>();
        Rigidbodies = AddComponentSystem<Rigidbody>();
        Sprites = AddComponentSystem<Sprite>();
        Bricks = AddComponentSystem<Brick>();
        Collisions = AddComponentSystem<Collision>();
        Transforms = AddComponentSystem<Transform>();
        
        Systems.Add(new BrickSystem(World, Bricks));
        Systems.Add(new PhysicsSystem(Clock, World, Rigidbodies, Transforms));
        Systems.Add(new AabbCollisionSystem(Clock, World, Rigidbodies, Collisions, BoxColliders, CircleColliders, Transforms));
        Systems.Add(new BallBrickCollisionSystem(World, Bricks, Collisions));
        
        SpawnBall();
        SpawnPaddle();
        SpawnBricks();
    }

    public void StartMovingPaddleLeft()
    {
        _movePaddleLeft = true;
    }
    
    public void StopMovingPaddleLeft()
    {
        _movePaddleLeft = false;
    }

    protected override void OnUpdate(float dt)
    {
        if (_movePaddleLeft || _movePaddleRight)
        {
            if (Rigidbodies.TryGetComponent(Paddle, out var rigidbody))
            {
                var left = _movePaddleLeft ? -100f : 0f;
                var right = _movePaddleRight ? 100f : 0f;
                rigidbody.Velocity = new Vector2(left + right, 0);
                Rigidbodies.UpdateComponent(Paddle, rigidbody);
            }
        }
        else
        {
            if (Rigidbodies.TryGetComponent(Paddle, out var rigidbody))
            {
                rigidbody.Velocity = Vector2.Zero;
                Rigidbodies.UpdateComponent(Paddle, rigidbody);
            }
        }
    }

    public void MovePaddleRight()
    {
        if (Rigidbodies.TryGetComponent(Paddle, out var rigidbody))
        {
            rigidbody.Velocity = new Vector2(100, 0);
            Rigidbodies.UpdateComponent(Paddle, rigidbody);
        }
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
        
        Transforms.AddComponent(ballEntity, new Transform
        {
            Position = new Vector2(320, 340)
        });
        
        rigidbodies.AddComponent(ballEntity, new Rigidbody
        {
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
        
        Transforms.AddComponent(entity, new Transform
        {
            Position = new Vector2(x, y)
        });
        
        Rigidbodies.AddComponent(entity, new Rigidbody
        {
            IsKinematic = true,
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
        Paddle = entity;
        
        Sprites.AddComponent(entity, new Sprite
        {
            Kind = SpriteKind.Paddle,
            Width = 120,
            Height = 25
        });
        
        Transforms.AddComponent(entity, new Transform
        {
            Position = new Vector2(320, 400)
        });
        
        Rigidbodies.AddComponent(entity, new Rigidbody
        {
            IsKinematic = false,
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
