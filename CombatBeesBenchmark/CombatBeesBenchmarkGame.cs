using EasyGameFramework.Api;

namespace CombatBeesBenchmark;

public class CombatBeesBenchmarkGame : Game
{
    private const int StartBeeCount = 1000;
    
    private Field Field { get; }
    private BeeSystem BeeSystem { get; }
    private BeeSpawner BeeSpawner { get; }
    
    public CombatBeesBenchmarkGame(IContext context) : base(context)
    {
        Field = new Field();
        BeeSystem = new BeeSystem(Context, Field, new BeeSystemConfig
        {
            MaxBeeCount = StartBeeCount,
            MinBeeSize = 0.25f,
            MaxBeeSize = 0.5f,
            FlightJitter = 200f,
            Damping = 0.9f,
        });
        BeeSpawner = new BeeSpawner(BeeSystem, StartBeeCount);
    }

    protected override void Configure()
    {
        var window = Window;
        window.Title = "Combat Bees Benchmark";
        window.SetScreenSize(1280, 720);
        window.IsVsyncEnabled = false;
    }

    protected override void OnStart()
    {
        BeeSystem.LoadResources();
    }

    protected override void OnUpdate()
    {
        var dt = Time.UpdateDeltaTime;
        BeeSpawner.Update(dt);
        BeeSystem.Update(dt);
    }

    protected override void OnRender()
    {
        BeeSystem.Render();
    }

    protected override void OnStop()
    {
    }
}