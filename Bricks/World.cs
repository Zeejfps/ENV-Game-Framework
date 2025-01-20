using Bricks.Entities;
using Bricks.Repos;

namespace Bricks;

public sealed class World
{
    public PaddleEntity Paddle { get; set; }
    public IClock Clock { get; }
    public BallsRepo Balls { get; }
    public BricksRepo Bricks { get; }
    public DynamicEntitiesRepo DynamicEntities { get; }

    private readonly IRepo[] _repos;
    
    public World(IClock clock)
    {
        Clock = clock;
        Balls = new BallsRepo();
        Bricks = new BricksRepo();
        DynamicEntities = new DynamicEntitiesRepo();
        _repos = new IRepo[]
        {
            Balls, Bricks, DynamicEntities
        };
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