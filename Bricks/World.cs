using Bricks.Entities;
using Bricks.Repos;

namespace Bricks;

public sealed class World
{
    public IClock Clock { get; }
    public PaddleEntity Paddle { get; }
    public BallsRepo Balls { get; }
    public BricksRepo Bricks { get; }

    private readonly IRepo[] _repos;
    
    public World(IClock clock, PaddleEntity paddle)
    {
        Clock = clock;
        Paddle = paddle;
        Balls = new BallsRepo();
        Bricks = new BricksRepo();
        _repos = new IRepo[]
        {
            Balls, Bricks
        };
    }

    public void Update()
    {
        foreach (var repo in _repos)
        {
            repo.Update();
        }
    }
}