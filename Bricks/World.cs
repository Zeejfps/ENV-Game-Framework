using System.Numerics;
using Bricks.Entities;
using Bricks.Repos;

namespace Bricks;

public sealed class World
{
    public AABB Arena { get; }
    public PaddleEntity Paddle { get; set; }
    public IClock Clock { get; }
    public BallsRepo Balls { get; }
    public BricksRepo Bricks { get; }
    public DynamicEntitiesRepo DynamicEntities { get; }

    private readonly IRepo[] _repos;
    
    public World(IClock clock)
    {
        Clock = clock;
        Arena = AABB.FromLeftTopWidthHeight(0, 0, 640, 480);
        Balls = new BallsRepo();
        Bricks = new BricksRepo();
        DynamicEntities = new DynamicEntitiesRepo();
        _repos = new IRepo[]
        {
            Balls, Bricks, DynamicEntities
        };
    }

    public BallEntity CreateBall()
    {
        return new BallEntity(this);
    }

    public PaddleEntity CreatePaddle()
    {
        return new PaddleEntity(this);
    }

    public BrickEntity CreateBrick(float x, float y, float width, float height)
    {
        var brick = new BrickEntity(this)
        {
            Position = new Vector2(x, y),
            Width = width,
            Height = height,
        };
        return brick;
    }

    public void Update()
    {
        foreach (var repo in _repos)
        {
            repo.Update();
        }

        foreach (var dynamicEntity in DynamicEntities.GetAll())
        {
            dynamicEntity.Update();
        }
    }
}